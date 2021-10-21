using PendleCodeMonkey.Z80EmulatorLib.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static PendleCodeMonkey.Z80EmulatorLib.Assembler.AssemblerEnumerations;

namespace PendleCodeMonkey.Z80EmulatorLib.Assembler
{
	/// <summary>
	/// Implementation of the <see cref="Assembler"/> class.
	/// </summary>
	public class Assembler
	{
		private readonly string[] _validRegs = { "A", "F", "B", "C", "D", "E", "H", "L",
									  "AF", "BC", "DE", "HL", "IX", "IY", "AF'", "SP", "I", "R"};

		private readonly string[] _validFlags = { "NZ", "Z", "NC", "C", "PE", "PO", "P", "M" };

		private readonly string _operators = "+-*/%";


		private int _currentAddress = 0;
		private int _currentLineNumber = 0;
		private bool _fatalErrorEncountered = false;

		private readonly Dictionary<string, string> _equates = new Dictionary<string, string>();
		private readonly Dictionary<string, int> _labels = new Dictionary<string, int>();
		private readonly List<AssemblerInstructionInfo> _instructions = new List<AssemblerInstructionInfo>();

		private List<(string instruction, OpcodePrefix prefix, byte opcodeKey)> _allInstructions = new List<(string instruction, OpcodePrefix prefix, byte opcodeKey)>();
		private List<string> _reservedWords = new List<string>();

		/// <summary>
		/// Initializes a new instance of the <see cref="Assembler"/> class.
		/// </summary>
		public Assembler()
		{
			Reset();
			GenerateAllInstructionList();
			GenerateReservedWordList();
		}

		/// <summary>
		/// Gets a collection of tuples containing details of errors that occurred during assembly.
		/// </summary>
		public List<(int lineNumber, Errors Error, string AdditionalInfo)> AsmErrors { get; private set; }

		/// <summary>
		/// Gets a collection of tuples containing information about data segments created during
		/// assembler operation (i.e. start address and length in bytes of each data segment).
		/// This information can then be fed into the disassembler to prevent if attempting to
		/// disassemble non-executable binary data.
		/// </summary>
		public List<(ushort startAddress, ushort size)> DataSegments { get; private set; }

		/// <summary>
		/// Run the source code (supplied as a list of strings) through the Z80 assembler.
		/// </summary>
		/// <remarks>
		/// This is the main entry point for operation of the assembler.
		/// </remarks>
		/// <param name="code">The Z80 assembly language source code as a collection of strings (one
		/// string per line of source code).</param>
		/// <returns>
		/// A tuple consisting of the following fields:
		///		Success - <c>true</c> if the assembler completed successfully; otherwise <c>false</c>.
		///		BinaryData - The generated binary data as a collection of bytes (only if successful)
		///	</returns>
		public (bool Success, List<byte> BinaryData) Assemble(List<string> code)
		{
			int lineNumber = 1;

			Reset();

			// First pass.
			// Parses directives and instructions from the supplied source code. Generates binary code with
			// placeholders where the operand values will be placed during the second pass.
			foreach (var line in code)
			{
				ParseLine(line, lineNumber);

				// If we have encountered a fatal error then there's little point continuing.
				if (_fatalErrorEncountered)
				{
					break;
				}

				// Increment the line number counter.
				lineNumber++;
			}

			// Second pass.
			// Evaluate the actual values of the operands (as this could not fully done during the first pass) - this fills in
			// the placeholders that were created during the first pass (which can now be done as the values of all
			// equates and labels, etc. should have been fully determined during the first pass).
			// Of course, we only perform the second pass if we have not encountered a fatal error during the first pass.
			if (!_fatalErrorEncountered)
			{
				EvaluateOperands();

				// Look for errors (specifically, any unresolved operands)
				foreach (var inst in _instructions)
				{
					if (inst.Operand1Type == OperandType.Unresolved || inst.Operand1Type == OperandType.UnresolvedIndirect)
					{
						LogError(Errors.UnresolvedOperandValue, inst.Operand1, inst.LineNumber);
					}
					if (inst.Operand2Type == OperandType.Unresolved || inst.Operand2Type == OperandType.UnresolvedIndirect)
					{
						LogError(Errors.UnresolvedOperandValue, inst.Operand2, inst.LineNumber);
					}
				}
			}

			bool success = false;
			List<byte> binaryData = new List<byte>();
			if (AsmErrors.Count == 0)
			{
				// No errors have been logged so we should be OK to generate the final binary data.
				// We call on each AssemberInstructionInfo object to generate its own binary data, adding it
				// to the final binary blob.
				foreach (var instruction in _instructions)
				{
					binaryData.AddRange(instruction.BinaryData);
				}
				success = true;
			}

			return (success, binaryData);
		}

		/// <summary>
		/// Reset the assembler internal data to its default settings.
		/// </summary>
		private void Reset()
		{
			_equates.Clear();
			_labels.Clear();
			_instructions.Clear();
			_fatalErrorEncountered = false;
			AsmErrors = new List<(int lineNumber, Errors Error, string AdditionalInfo)>();
			DataSegments = new List<(ushort startAddress, ushort size)>();
		}

		/// <summary>
		/// Parse a single line of Z80 assembly language source code.
		/// </summary>
		/// <param name="line">A string containing a single line of Z80 assembly language source code.</param>
		/// <param name="lineNumber">The number of this line in the source code.</param>
		private void ParseLine(string line, int lineNumber)
		{
			_currentLineNumber = lineNumber;

			if (string.IsNullOrEmpty(line) || line[0] == ';')
			{
				// An empty line or a commented out line, so ignore it.
				return;
			}

			// Use a regular expression to tokenize the line of text.
			Regex regex = new Regex(@"(?<tkn>^[\w$_:]*)|((?<tkn>[\w$_.()-]+\s[+/*-]\s[\w$_.()-]+)|(?<tkn>[""'(](.*?)(?<!\\)[""')]|(?<tkn>[\w'$&%_=+*/.()-]+))(\s)*)|(?<cmt>;.*)", RegexOptions.None);

			var tokens = (from Match m in regex.Matches(line)
						  where m.Groups["tkn"].Success
						  select m.Groups["tkn"].Value).ToList();

			var comment = (from Match m in regex.Matches(line)
						   where m.Groups["cmt"].Success
						   select m.Groups["cmt"].Value).ToList();

			if (tokens.Count > 1)
			{
				// Check for special case of character constant immediately followed by a token starting with a '+' (i.e. a potential
				// arithmetic operation on a character constant) - to cover strings such as "'A'+$80".
				int tokenNum = 1;
				while (tokenNum < tokens.Count)
				{
					if (tokens[tokenNum].Length == 3 && tokens[tokenNum][0] == '\'' && tokens[tokenNum][2] == '\'')
					{
						if (tokenNum < tokens.Count - 1 && tokens[tokenNum + 1].Length > 1 && tokens[tokenNum + 1].StartsWith('+'))
						{
							// Join the character constant token with the arithmetic operation token and remove the
							// artihmetic operation token (i.e. merge the two tokens into a single token)
							tokens[tokenNum] += tokens[tokenNum + 1];
							tokens.RemoveAt(tokenNum + 1);
						}
					}
					tokenNum++;
				}
			}

			// Perform some processing of potential arithmetic operations.
			if (tokens.Count > 2)
			{
				int index = 2;
				while (index < tokens.Count)
				{
					int operatorIndex = -1;
					int tokNum = index;
					for (; tokNum < tokens.Count; tokNum++)
					{
						if (tokens[tokNum].Length == 1 && _operators.Contains(tokens[tokNum]))
						{
							operatorIndex = tokNum;
							break;
						}
					}

					if (operatorIndex >= 0)
					{
						while (tokens.Count > operatorIndex)
						{
							if (tokens[operatorIndex].Length == 1 && _operators.Contains(tokens[operatorIndex]))
							{
								tokens[operatorIndex - 1] += tokens[operatorIndex] + tokens[operatorIndex + 1];

								tokens.RemoveAt(operatorIndex + 1);
								tokens.RemoveAt(operatorIndex);

							}
							else
							{
								break;
							}
						}
					}
					index = tokNum + 1;
				}
			}

			if (tokens.Count >= 1)
			{
				string label = null;

				// If the first token ends with a colon then treat it as a label.
				if (tokens[0].EndsWith(':'))
				{
					label = tokens[0].TrimEnd(':');
				}

				// Determine if this line is an EQU
				if (tokens.Count > 2)
				{
					if (tokens[1].ToUpper().Equals("EQU") || tokens[1].Equals("="))
					{
						string equName = label ?? tokens[0];
						if (_reservedWords.Contains(equName))
						{
							LogError(Errors.EQUNameCannotBeReservedWord, equName);
						}
						else
						{
							if (!_equates.ContainsKey(equName))
							{
								_equates.Add(equName, tokens[2]);
							}
							else
							{
								LogError(Errors.CannotRedefineEquValue, equName);
							}
						}

						// We've handled the EQU and therefore finished with this line of source code, so just return.
						return;
					}
				}

				// If this line contains a label (that is not an EQU) then handle it as a label for the current address.
				if (label != null)
				{
					if (_reservedWords.Contains(label))
					{
						LogError(Errors.LabelNameCannotBeReservedWord, label);
					}
					else
					{
						if (!_labels.ContainsKey(label))
						{
							_labels.Add(label, _currentAddress);
						}
						else
						{
							LogError(Errors.CannotHaveDuplicateLabelNames, label);
						}
					}

					// We remove token[0] as it is a label that has now been handled.
					tokens.RemoveAt(0);
				}
			}

			if (tokens.Count > 0 && string.IsNullOrWhiteSpace(tokens[0]))
			{
				// We remove token[0] as it only contains whitespace (and can therefore be ignored).
				tokens.RemoveAt(0);
			}

			if (tokens.Count == 0)
			{
				// No more tokens on this line so there is nothing more to be done.
				return;
			}

			// Try to handle the first token as an assembler directive or as a Z80 instruction.
			string cmd = tokens[0].ToUpper();
			switch (cmd)
			{
				case "ORG":
					if (tokens.Count > 1)
					{
						var orgAddress = GetAsNumber(tokens[1]);
						if (orgAddress.HasValue)
						{
							if (orgAddress.Value >= 0 && orgAddress.Value <= 0xFFFF)
							{
								_currentAddress = orgAddress.Value;
							}
							else
							{
								LogError(Errors.OrgAddressOutOfValidRange, tokens[1]);
							}
						}
						else
						{
							LogError(Errors.InvalidOrgAddress, tokens[1]);
						}
					}
					break;
				case "DB":
				case "DEFB":
				case "DM":
				case "DEFM":
					DefineByteSegment(tokens, null);
					break;
				case "DW":
				case "DEFW":
					DefineWordSegment(tokens, null);
					break;
				case "DS":
				case "DEFS":
					DefineSpaceSegment(tokens);
					break;

				default:
					// If not recognised as an assembler directive then try to handle as a Z80 instruction with operands
					var (inst, operand1, opType1, operand2, opType2) = GetOpcode(tokens);
					if (inst != null)
					{
						var asmInstrInfo = new AssemblerInstructionInfo(lineNumber, (ushort)_currentAddress, inst, operand1, opType1, operand2, opType2);
						var binData = asmInstrInfo.GenerateBinaryData();
						_instructions.Add(asmInstrInfo);
						_currentAddress += binData.Count;
					}
					else
					{
						LogError(Errors.InvalidInstruction, line);
					}
					break;
			}

			// Check if the current assembly address has gone beyond the range of memory, flagging a fatal
			// error if it does.
			if (_currentAddress > 0xFFFF)
			{
				LogError(Errors.CurrentAddressOutOfRange, _currentAddress.ToString());
				_fatalErrorEncountered = true;
			}
		}

		/// <summary>
		/// Define a byte data segment (for example, for a DB or DEFB directive).
		/// </summary>
		/// <remarks>
		/// During the first pass each element of the data segment will be populated with an empty
		/// placeholder (except for quoted strings, whose value can be determined during the first pass)
		/// this is because it may not be possible to fully resolve the final element value until after
		/// completion of the first pass (when the values of all EQUs and labels have been determined).
		/// </remarks>
		/// <param name="tokens">Collection of token strings making up this data segment.</param>
		/// <param name="aii">
		///		An <see cref="AssemblerInstructionInfo"/> instance corresponding to this data segment.
		///		Will be null during the first pass but will contain a valid instance during the second pass.
		///	</param>
		private void DefineByteSegment(List<string> tokens, AssemblerInstructionInfo aii)
		{
			if (aii != null && aii.DataSegment != null)
			{
				tokens = aii.DataSegment.Tokens;
			}

			if (tokens.Count < 2)
			{
				return;
			}

			List<byte> dataSegment = new List<byte>();
			for (int tokenNum = 1; tokenNum < tokens.Count; tokenNum++)
			{
				string token = tokens[tokenNum];

				// Check if token is a quoted string (either single or double quotes can be used)
				if (token.Length > 2 && (token[0] == '\"' || token[0] == '\'') && (token[^1] == '\"' || token[^1] == '\''))
				{
					string quotedString = token[1..^1];
					foreach (var chr in quotedString)
					{
						dataSegment.Add((byte)chr);
					}
				}
				else
				{
					// Not a quoted string so we expect a numeric value.
					byte dataValue = 0;

					// Only evaluate the tokens on the second pass (when we have been supplied a non-null AssemblerInstructionInfo object).
					// On the first pass we just insert placeholder bytes (with zero value).
					if (aii != null)
					{
						var (evaluated, value) = Evaluate(token);
						if (evaluated)
						{
							if (value >= -128 && value <= 255)
							{
								dataValue = (byte)(value & 0xFF);
							}
							else
							{
								LogError(Errors.ByteSegmentValueOutOfRange, token);
							}
						}
						else
						{
							LogError(Errors.InvalidByteSegmentValue, token);
						}
					}

					dataSegment.Add(dataValue);
				}
			}

			var dsi = new DataSegmentInfo(DataSegmentType.Byte, tokens, dataSegment);
			if (aii != null)
			{
				aii.DataSegment = dsi;
				_ = aii.GenerateBinaryData();
			}
			else
			{
				var asmInstrInfo = new AssemblerInstructionInfo(_currentLineNumber, (ushort)_currentAddress, null, null, OperandType.None, null, OperandType.None, dsi);
				var binData = asmInstrInfo.GenerateBinaryData();
				_instructions.Add(asmInstrInfo);
				DataSegments.Add(((ushort)_currentAddress, (ushort)binData.Count));
				_currentAddress += binData.Count;
			}
		}

		/// <summary>
		/// Define a word data segment (for example, for a DW or DEFW directive).
		/// </summary>
		/// <remarks>
		/// During the first pass each element of the data segment will be populated with an empty
		/// placeholder (except for quoted strings, whose value can be determined during the first pass)
		/// this is because it may not be possible to fully resolve the final element value until after
		/// completion of the first pass (when the values of all EQUs and labels have been determined).
		/// </remarks>
		/// <param name="tokens">Collection of token strings making up this data segment.</param>
		/// <param name="aii">
		///		An <see cref="AssemblerInstructionInfo"/> instance corresponding to this data segment.
		///		Will be null during the first pass but will contain a valid instance during the second pass.
		///	</param>
		private void DefineWordSegment(List<string> tokens, AssemblerInstructionInfo aii)
		{
			if (aii != null && aii.DataSegment != null)
			{
				tokens = aii.DataSegment.Tokens;
			}

			if (tokens.Count < 2)
			{
				return;
			}

			List<byte> dataSegment = new List<byte>();
			for (int tokenNum = 1; tokenNum < tokens.Count; tokenNum++)
			{
				ushort dataValue = 0;

				// Only evaluate the tokens on the second pass (when we have been supplied a non-null AssemblerInstructionInfo object).
				// On the first pass we just insert placeholder words (with zero value).
				if (aii != null)
				{
					var (evaluated, value) = Evaluate(tokens[tokenNum]);
					if (evaluated)
					{
						if (value >= -32768 && value <= 65535)
						{
							dataValue = (ushort)(value & 0xFFFF);
						}
						else
						{
							LogError(Errors.WordSegmentValueOutOfRange, tokens[tokenNum]);
						}
					}
					else
					{
						LogError(Errors.InvalidWordSegmentValue, tokens[tokenNum]);
					}
				}

				dataSegment.Add((byte)dataValue);
				dataSegment.Add((byte)(dataValue >> 8 & 0xFF));
			}

			var dsi = new DataSegmentInfo(DataSegmentType.Word, tokens, dataSegment);
			if (aii != null)
			{
				aii.DataSegment = dsi;
				_ = aii.GenerateBinaryData();
			}
			else
			{
				var asmInstrInfo = new AssemblerInstructionInfo(_currentLineNumber, (ushort)_currentAddress, null, null, OperandType.None, null, OperandType.None, dsi);
				var binData = asmInstrInfo.GenerateBinaryData();
				_instructions.Add(asmInstrInfo);
				DataSegments.Add(((ushort)_currentAddress, (ushort)binData.Count));
				_currentAddress += binData.Count;
			}
		}

		/// <summary>
		/// Define a space data segment (for example, for a DS or DEFS directive).
		/// </summary>
		/// <remarks>
		/// This data segment must be fully resolved during the first pass (as the correct amount
		/// of memory space needs to be allocated for it during the first pass).
		/// </remarks>
		/// <param name="tokens">Collection of token strings making up this data segment.</param>
		private void DefineSpaceSegment(List<string> tokens)
		{
			ushort size = 0;
			byte initValue = 0;

			if (tokens.Count < 2)
			{
				return;
			}

			// Tokens must be evaluated during the first pass (as we need to know how many
			// bytes we need to allocate during the first pass)
			var (evaluated, sizeValue) = Evaluate(tokens[1]);
			if (evaluated)
			{
				if (sizeValue >= 1 && sizeValue <= 65535)
				{
					size = (ushort)(sizeValue & 0xFFFF);
				}
				else
				{
					LogError(Errors.SpaceSegmentSizeOutOfRange, tokens[1]);
				}
			}
			else
			{
				LogError(Errors.SpaceSegmentInvalidParameter, tokens[1]);
			}

			if (tokens.Count > 2)
			{
				var (eval, value) = Evaluate(tokens[2]);
				if (eval)
				{
					if (value >= -128 && value <= 255)
					{
						initValue = (byte)(value & 0xFF);
					}
					else
					{
						LogError(Errors.SpaceSegmentInitializeValueOutOfRange, tokens[2]);
					}
				}
				else
				{
					LogError(Errors.SpaceSegmentInvalidParameter, tokens[2]);
				}

			}

			List<byte> dataSegment = Enumerable.Repeat(initValue, size).ToList();

			var dsi = new DataSegmentInfo(DataSegmentType.Space, tokens, dataSegment);

			var asmInstrInfo = new AssemblerInstructionInfo(_currentLineNumber, (ushort)_currentAddress, null, null, OperandType.None, null, OperandType.None, dsi);
			var binData = asmInstrInfo.GenerateBinaryData();
			_instructions.Add(asmInstrInfo);
			DataSegments.Add(((ushort)_currentAddress, (ushort)binData.Count));
			_currentAddress += binData.Count;
		}

		/// <summary>
		/// Log an assembler error.
		/// </summary>
		/// <param name="error">Enumerated value of the error to be logged.</param>
		/// <param name="additionalInfo">A string containing additional information related to the error (e.g. invalid operand value, etc.)</param>
		/// <param name="lineNumber">The line number in the source code where the error occurs.</param>
		private void LogError(Errors error, string additionalInfo, int? lineNumber = null)
		{
			AsmErrors.Add((lineNumber ?? _currentLineNumber, error, additionalInfo));
		}

		/// <summary>
		/// Evaluate the values of the operand.
		/// </summary>
		/// <remarks>
		/// This method performs virtually all of the operations required for the second pass of the
		/// assembler (i.e. calculating the final values of operands after all EQUs and label values
		/// have been determined by the first pass).
		/// </remarks>
		private void EvaluateOperands()
		{
			foreach (var instruction in _instructions)
			{
				_currentAddress = instruction.Address;

				// If the AssemblerInstructionInfo object corresponds to data segment then
				// determine that segments values.
				if (instruction.DataSegment != null)
				{

					if (instruction.DataSegment.Type == DataSegmentType.Byte)
					{
						DefineByteSegment(null, instruction);
					}
					else if (instruction.DataSegment.Type == DataSegmentType.Word)
					{
						DefineWordSegment(null, instruction);
					}
				}
				else
				{
					// Not a data segment so evaluate the individual operands for the Z80 instruction.
					EvaluateOperand(instruction, 1);
					EvaluateOperand(instruction, 2);
				}

				// Regenerate the binary data (to include the actual operand values instead of the placeholders from the first pass)
				instruction.GenerateBinaryData();
			}
		}

		/// <summary>
		/// Evaluate a single Z80 instruction operand.
		/// </summary>
		/// <param name="aii">The <see cref="AssemblerInstructionInfo"/> instance corresponding to the Z80 instruction.</param>
		/// <param name="operandNum">The operand number (1 for the first operand, or 2 for the second operand).</param>
		private void EvaluateOperand(AssemblerInstructionInfo aii, int operandNum)
		{
			OperandType opType = operandNum == 1 ? aii.Operand1Type : aii.Operand2Type;
			string operand = operandNum == 1 ? aii.Operand1 : aii.Operand2;

			if (opType == OperandType.Immediate || opType == OperandType.Unresolved)
			{
				var (evaluated, value) = Evaluate(operand);
				if (evaluated)
				{
					if (aii.Instruction.Info.AddrMode1 == AddrMode.Immediate ||
							aii.Instruction.Info.AddrMode2 == AddrMode.Immediate)
					{
						if (value >= -128 && value <= 255)
						{
							aii.Instruction.ByteOperand = (byte)(value & 0xFF);
						}
						else
						{
							LogError(Errors.OperandValueOutOfRange, operand, aii.LineNumber);
						}
					}
					else if (aii.Instruction.Info.AddrMode1 == AddrMode.ExtendedImmediate ||
						aii.Instruction.Info.AddrMode2 == AddrMode.ExtendedImmediate)
					{
						if (value >= -32768 && value <= 65535)
						{
							aii.Instruction.WordOperand = (ushort)(value & 0xFFFF);
						}
						else
						{
							LogError(Errors.OperandValueOutOfRange, operand, aii.LineNumber);
						}
					}
					if (operandNum == 1)
					{
						aii.Operand1Type = OperandType.Immediate;
					}
					else
					{
						aii.Operand2Type = OperandType.Immediate;
					}
				}
				else
				{
					if (operandNum == 1)
					{
						aii.Operand1Type = OperandType.Unresolved;
					}
					else
					{
						aii.Operand2Type = OperandType.Unresolved;
					}
				}
			}

			if (opType == OperandType.Indirect || opType == OperandType.UnresolvedIndirect)
			{
				var (evaluated, value) = Evaluate(operand[1..^1]);
				if (evaluated)
				{
					if (aii.Instruction.Info.AddrMode1 == AddrMode.Immediate ||
							aii.Instruction.Info.AddrMode2 == AddrMode.Immediate)
					{
						if (value >= -128 && value <= 255)
						{
							aii.Instruction.ByteOperand = (byte)(value & 0xFF);
						}
						else
						{
							LogError(Errors.OperandValueOutOfRange, operand, aii.LineNumber);
						}
					}
					else if (aii.Instruction.Info.AddrMode1 == AddrMode.ExtendedImmediate ||
						aii.Instruction.Info.AddrMode2 == AddrMode.ExtendedImmediate)
					{
						if (value >= -32768 && value <= 65535)
						{
							aii.Instruction.WordOperand = (ushort)(value & 0xFFFF);
						}
						else
						{
							LogError(Errors.OperandValueOutOfRange, operand, aii.LineNumber);
						}
					}
					if (operandNum == 1)
					{
						aii.Operand1Type = OperandType.Indirect;
					}
					else
					{
						aii.Operand2Type = OperandType.Indirect;
					}
				}
				else
				{
					if (operandNum == 1)
					{
						aii.Operand1Type = OperandType.UnresolvedIndirect;
					}
					else
					{
						aii.Operand2Type = OperandType.UnresolvedIndirect;
					}
				}
			}

			if (opType == OperandType.Indexed)
			{
				string displacement = operand[3..^1];

				var (evaluated, value) = Evaluate(displacement);
				if (evaluated)
				{
					if (value < -128 || value > 127)
					{
						LogError(Errors.DisplacementOutOfRange, operand, aii.LineNumber);
					}
					else
					{
						aii.Instruction.Displacement = (byte)value;
					}
				}
			}

			if (opType == OperandType.Relative)
			{
				var (evaluated, value) = Evaluate(operand);
				if (evaluated)
				{
					var displacement = value - aii.Address;
					if (displacement < (-128 + aii.BinaryData.Count) || displacement > (127 + aii.BinaryData.Count))
					{
						LogError(Errors.DisplacementOutOfRange, operand, aii.LineNumber);
					}
					else
					{
						aii.Instruction.Displacement = (byte)(displacement - aii.BinaryData.Count);
					}
				}
			}
		}

		/// <summary>
		/// Attempt to retrieve details for a Z80 instruction.
		/// </summary>
		/// <param name="tokens">Collection of strings containing the tokens making up the instruction.</param>
		/// <returns>
		/// A tuple consisting of the following elements:
		///		inst - An <see cref="Instruction"/> object containing details of the instruction (or null if not a valid instruction)
		///		operand1 - A string containing the first operand.
		///		op1Type - Enumerated type of the first operand.
		///		operand2 - A string containing the second operand.
		///		op2Type - Enumerated type of the second operand.
		/// </returns>
		private (Instruction inst, string operand1, OperandType op1Type, string operand2, OperandType op2Type) GetOpcode(List<string> tokens)
		{
			string operand1 = null;
			string operand2 = null;

			if (tokens == null || tokens.Count == 0)
			{
				return default;
			}

			string instruction = tokens[0].ToUpper();

			if (tokens.Count > 1)
			{
				operand1 = tokens[1];
			}
			if (tokens.Count > 2)
			{
				operand2 = tokens[2];
			}

			// Try to evaluate the operands in case they contain arithmetic operations. If they can be
			// successfully evaluated then replace the operand with its calculated value.
			var (evaluated, value) = Evaluate(operand1);
			if (evaluated)
			{
				if (operand1[0] == '(' && operand1[^1] == ')')
				{
					operand1 = $"({value})";
				}
				else
				{
					operand1 = value.ToString();
				}
			}
			(evaluated, value) = Evaluate(operand2);
			if (evaluated)
			{
				if (operand2[0] == '(' && operand2[^1] == ')')
				{
					operand2 = $"({value})";
				}
				else
				{
					operand2 = value.ToString();
				}
			}

			// Modify the operands to include characters that match the values in the instruction tables (e.g.
			// replacing numeric operands with 'n' or 'nn', relative displacement values with 'd', etc.)
			string modifiedInstruction = instruction;
			var (modOp1, opType1) = GetModifiedOperandForInstructionSearch(instruction, operand1);
			if (!string.IsNullOrEmpty(modOp1))
			{
				modifiedInstruction += " " + modOp1;
			}
			var (modOp2, opType2) = GetModifiedOperandForInstructionSearch(instruction, operand2);
			if (!string.IsNullOrEmpty(modOp2))
			{
				modifiedInstruction += "," + modOp2;
			}

			// Attempt to find the Instruction object corresponding to the supplied opcode and operands
			Instruction foundInst = FindInstruction(modifiedInstruction);
			if (foundInst == null && modifiedInstruction.Contains('n'))
			{
				// If we failed to find a matching Instruction object and the operands being searched for
				// include a lower case letter 'n' then replace it with 'nn' and search again (in case the
				// instruction takes a 16-bit operand rather than an 8-bit operand)
				modifiedInstruction = modifiedInstruction.Replace("n", "nn");
				foundInst = FindInstruction(modifiedInstruction);
			}

			return (foundInst, operand1, opType1, operand2, opType2);
		}

		/// <summary>
		/// Get a modified operand string for use when searching for a matching Z80 instruction in the
		/// instruction tables.
		/// </summary>
		/// <param name="instruction">String containing the instruction mnemonic.</param>
		/// <param name="operand">String containing the operand value.</param>
		/// <returns>A tuple consisting of the following:
		///		modOperand - The modified operand string (e.g. the "+20" in "IX+20" will be modified to "+d", giving "IX+d", etc.)
		///		opType - Enumerated type of the operand.
		/// </returns>
		private (string modOperand, OperandType opType) GetModifiedOperandForInstructionSearch(string instruction, string operand)
		{
			string modifiedOperand = null;
			OperandType opType = OperandType.None;

			if (!string.IsNullOrEmpty(operand))
			{
				string upperCaseOperand = operand.ToUpper();

				// Check if the operand is a valid register or a valid flag.
				if (IsValidRegister(upperCaseOperand))
				{
					modifiedOperand = upperCaseOperand;
					opType = upperCaseOperand.Length == 2 ? OperandType.RegisterPair : OperandType.Register;
				}
				else if (IsValidFlagCondition(upperCaseOperand))
				{
					modifiedOperand = upperCaseOperand;
					opType = OperandType.Flag;
				}
				else
				{
					// Check if the operand is using an indirect addressing mode
					if (operand[0] == '(' && operand[^1] == ')')
					{
						string indirectOp = upperCaseOperand[1..^1];

						// Check if the string inside the brackets is a register - e.g. "(HL)".
						if (IsValidRegister(indirectOp))
						{
							if ((indirectOp.Equals("IX") || indirectOp.Equals("IY")) && !instruction.Equals("JP"))
							{
								// (IX) and (IY) are actually (IX+0) and (IY+0) - except when used for a JP instruction, e.g. JP (IX)
								modifiedOperand = $"({indirectOp}+d)";
								opType = OperandType.Indexed;
							}
							else
							{
								modifiedOperand = upperCaseOperand;
								opType = indirectOp.Length == 2 ? OperandType.RegisterPairIndirect : OperandType.RegisterIndirect;
							}
						}
						else
						{
							if (indirectOp.StartsWith("IX+") || indirectOp.StartsWith("IY+") ||
								indirectOp.StartsWith("IX-") || indirectOp.StartsWith("IY-"))
							{
								string displacement = indirectOp[3..];
								var disp = GetAsNumber(displacement);
								if (disp.HasValue)
								{
									modifiedOperand = $"({indirectOp[..2]}+d)";
									opType = OperandType.Indexed;
								}
							}

							if (modifiedOperand == null)
							{
								// Check if the string inside the brackets is a numeric value - e.g. "(20000)"
								var asNumber = GetAsNumber(indirectOp);
								if (asNumber.HasValue)
								{
									modifiedOperand = (instruction.Equals("IN") || instruction.Equals("OUT")) ? "(n)" : "(nn)";
									opType = OperandType.Indirect;
								}
							}
						}
						if (opType == OperandType.None)
						{
							modifiedOperand = "(nn)";
							opType = OperandType.UnresolvedIndirect;
						}
					}
					else
					{
						if (instruction.Equals("DJNZ") || instruction.StartsWith("JR"))
						{
							// Replace operand with an 'e'
							modifiedOperand = "e";
							opType = OperandType.Relative;
						}
						else
						{
							// Check if the operand string is a numeric value - e.g. "4000h"
							var asNumber = GetAsNumber(operand);
							if (asNumber.HasValue)
							{
								// If instruction is RST then use the hex value of the numeric operand.
								if (instruction.Equals("RST"))
								{
									modifiedOperand = $"&{asNumber:X2}";
									opType = OperandType.Implied;
								}
								// If instruction is BIT, RES, SET, or IM then use the decimal value of the numeric operand.
								else if (instruction.Equals("BIT") || instruction.Equals("RES") || instruction.Equals("SET") ||
									instruction.Equals("IM"))
								{
									modifiedOperand = $"{asNumber}";
									opType = OperandType.Implied;
								}
								else
								{
									// Otherwise replace it with an 'n'
									modifiedOperand = "n";
									opType = OperandType.Immediate;
								}
							}
							if (opType == OperandType.None)
							{
								modifiedOperand = "n";
								opType = OperandType.Unresolved;
							}
						}
					}
				}
			}

			return (modifiedOperand, opType);
		}

		/// <summary>
		/// Search for an entry in the instruction tables that matches the specified instruction.
		/// </summary>
		/// <param name="instruction">The instruction to be searched for in the instruction tables.</param>
		/// <returns>The matching <see cref="Instruction"/> object, or null if no match was found.</returns>
		private Instruction FindInstruction(string instruction)
		{
			Instruction instruct = null;
			Dictionary<OpcodePrefix, Dictionary<byte, InstructionInfo>> tables = new Dictionary<OpcodePrefix, Dictionary<byte, InstructionInfo>>
			{
				{ OpcodePrefix.None, InstructionTables.Unprefixed_Instructions },
				{ OpcodePrefix.CB, InstructionTables.CBPrefixed_Instructions },
				{ OpcodePrefix.ED, InstructionTables.EDPrefixed_Instructions },
				{ OpcodePrefix.DD, InstructionTables.DDPrefixed_Instructions },
				{ OpcodePrefix.FD, InstructionTables.FDPrefixed_Instructions },
				{ OpcodePrefix.DDCB, InstructionTables.DDCBPrefixed_Instructions },
				{ OpcodePrefix.FDCB, InstructionTables.FDCBPrefixed_Instructions }
			};

			(string instr, OpcodePrefix prefix, byte opcodeKey) foundInstruction = default;

			// Perform a binary search to look for the instruction in the list of all instructions.
			int min = 0;
			int max = _allInstructions.Count - 1;
			while (min <= max)
			{
				int mid = (min + max) / 2;
				int cmp = string.Compare(instruction, _allInstructions[mid].instruction);
				if (cmp == 0)
				{
					foundInstruction = _allInstructions[mid];
					break;
				}
				else if (cmp < 0)
				{
					max = mid - 1;
				}
				else
				{
					min = mid + 1;
				}
			}

			// if the instruction was found then build the object to be returned to the caller.
			if (foundInstruction != default)
			{
				var tab = tables[foundInstruction.prefix];
				var inst = tab[foundInstruction.opcodeKey];
				instruct = new Instruction(foundInstruction.opcodeKey, foundInstruction.prefix, inst, 0, 0, 0);
			}

			return instruct;
		}

		/// <summary>
		/// Determine if the supplied string is a valid register name (e.g. A, HL, IX, etc.).
		/// </summary>
		/// <param name="data">The string to be checked.</param>
		/// <returns><c>true</c> if the supplied string is a register name, otherwise <c>false</c>.</returns>
		private bool IsValidRegister(string data) => _validRegs.Where(x => x.Equals(data)).Any();

		/// <summary>
		/// Determine if the supplied string is a valid flag condition (e.g. NZ, C, PE, etc.).
		/// </summary>
		/// <param name="data">The string to be checked.</param>
		/// <returns><c>true</c> if the supplied string is a flag condition, otherwise <c>false</c>.</returns>
		private bool IsValidFlagCondition(string data) => _validFlags.Where(x => x.Equals(data)).Any();

		/// <summary>
		/// Attempt to get the specified string data as a numeric value.
		/// </summary>
		/// <remarks>
		/// This method handles values specified as decimal, binary or hexadecimal strings, as character constants, or
		/// as values that correspond to registered EQUs and labels.
		/// </remarks>
		/// <param name="data">The string to be converted to a numeric value.</param>
		/// <returns>The integer numeric value if successfully converted, otherwise null.</returns>
		private int? GetAsNumber(string data)
		{
			string dataWithoutSign = data;
			int multiplier = 1;

			// If the data starts with a '-' then set the multiplier to -1 and strip off the
			// first character (so the number we will get will be positive and when we multiply by
			// the multiplier, we get a negative number).
			if (data[0] == '-')
			{
				multiplier = -1;
				dataWithoutSign = data[1..];
			}
			else if (data[0] == '+')
			{
				// If the data starts with a '+' then just strip it off.
				dataWithoutSign = data[1..];
			}

			// Check if decimal number
			Match match = Regex.Match(data, @"^([-+]?[0-9]+)$");
			int? num;
			if (match.Success)
			{
				try
				{
					num = int.Parse(match.Groups[1].Value);
					return num;
				}
				catch { }
			}

			// Check if hexadecimal number (which can be prefixed with an ampersand or a dollar sign,
			// or followed by H or h).
			match = Regex.Match(dataWithoutSign, @"^[&$]([0-9A-Fa-f]+)$");
			if (match.Success)
			{
				try
				{
					num = Convert.ToInt32(match.Groups[1].Value, 16);
					return num * multiplier;
				}
				catch { }
			}

			match = Regex.Match(dataWithoutSign, @"^([0-9A-Fa-f]+)[Hh]$");
			if (match.Success)
			{
				try
				{
					num = Convert.ToInt32(match.Groups[1].Value, 16);
					return num * multiplier;
				}
				catch { }
			}


			// Check if binary number (which can be prefixed with a percentage sign
			// or followed by B or b).
			match = Regex.Match(data, @"^[%]([01]+)$");
			if (match.Success)
			{
				try
				{
					num = Convert.ToInt32(match.Groups[1].Value, 2);
					return num;
				}
				catch { }
			}

			match = Regex.Match(data, @"^([01]+)[Bb]+$");
			if (match.Success)
			{
				try
				{
					num = Convert.ToInt32(match.Groups[1].Value, 2);
					return num;
				}
				catch { }
			}

			//Check if a char constant
			if (data.Length == 3 && data[0] == '\'' && data[2] == '\'')
			{
				int val = data[1];
				return val;
			}

			// Doesn't seem to be a numeric value or a character constant so check if it corresponds to a registered EQU.
			if (_equates.ContainsKey(dataWithoutSign))
			{
				// Try to evaluate it assuming it might contain some arithmetic operations.
				var (evaluated, value) = Evaluate(_equates[dataWithoutSign]);
				if (evaluated)
				{
					return value * multiplier;
				}
			}

			// Check if data is a registered label name.
			if (_labels.ContainsKey(data))
			{
				return _labels[data];
			}

			// The current address should be returned if the token is a dollar sign.
			if (data.Equals("$"))
			{
				return _currentAddress;
			}

			return null;
		}

		/// <summary>
		/// Determine if the specified character is one of the supported arithmetic operators (+, -, *, /, or %).
		/// </summary>
		/// <param name="ch"></param>
		/// <returns></returns>
		private bool IsOperator(char ch) => _operators.Contains(ch);

		/// <summary>
		/// Perform simple tokenization of arithmetic expressions.
		/// </summary>
		/// <param name="expression">The expression to be tokenized.</param>
		/// <returns>A collection of strings containing the tokens extracted from the supplied expression.</returns>
		private List<string> ArithmeticLexer(string expression)
		{
			List<string> tokens = new List<string>();
			StringBuilder sb = new StringBuilder();

			// Replace double operators with corresponding single operators (e.g. replace a double-minus "--" with a plus "+", etc.)
			expression = expression.Replace("--", "+");
			expression = expression.Replace("+-", "-");
			expression = expression.Replace("-+", "-");

			for (var i = 0; i < expression.Length; i++)
			{
				var ch = expression[i];

				// Ignore any whitespace characters.
				if (char.IsWhiteSpace(ch))
				{
					continue;
				}

				if (!IsOperator(ch))
				{
					sb.Append(ch);

					while (i + 1 < expression.Length && (!IsOperator(expression[i + 1])))
					{
						sb.Append(expression[++i]);
					}

					tokens.Add(sb.ToString());
					sb.Clear();
					continue;
				}

				tokens.Add(ch.ToString());
			}

			// if the first token is a '-' or a '+' then merge it into the second token and remove the first (so
			// an expression starting with tokens "-" and "20" will have them merged so it will start with a single
			// token of "-20")
			if (tokens.Count > 1 && (tokens[0] == "-" || tokens[0] == "+"))
			{
				tokens[1] = tokens[0] + tokens[1];
				tokens.RemoveAt(0);
			}

			return tokens;
		}

		/// <summary>
		/// Attempt to evaluate a given expression.
		/// </summary>
		/// <remarks>
		/// This method provides support for evaluating expressions that may contain arithmetic operations.
		/// </remarks>
		/// <param name="expression">The expression to be evaluated.</param>
		/// <returns>A tuple consisting of the following:
		///		evaluated - <c>true</c> if the expression was successfully evaluated, otherwise <c>false</c>.
		///		value - The result of the evaluation (if successful)
		/// </returns>
		private (bool evaluated, int value) Evaluate(string expression)
		{
			// Nothing to do if the expression is empty.
			if (string.IsNullOrEmpty(expression))
			{
				return (false, 0);
			}

			// If the expression is enclosed within brackets then evaluate the value within those brackets.
			if (expression[0] == '(' && expression[^1] == ')')
			{
				expression = expression[1..^1];
			}

			// Try to evaluate the expression as a number as-is (because it might not contain any arithmetic operators)
			var equNum = GetAsNumber(expression);
			if (equNum.HasValue)
			{
				return (true, equNum.Value);
			}

			// The expression could not simply be converted to a number as-is so treat it as though it may
			// contain arithmetic operations.
			bool evaluated = true;
			int value = 0;
			var tokens = ArithmeticLexer(expression);

			var numOperators = tokens.Where(x => x.Length == 1 && IsOperator(x[0])).Count();
			if (numOperators > 0)
			{
				var asNumber = GetAsNumber(tokens[0].Trim());
				if (asNumber.HasValue)
				{
					value = asNumber.Value;
				}
				else
				{
					evaluated = false;
				}

				int tokNum = 1;
				while (tokNum < tokens.Count)
				{
					string op = tokens[tokNum++];
					if (tokNum < tokens.Count)
					{
						string tokVal = tokens[tokNum++];
						asNumber = GetAsNumber(tokVal.Trim());
						if (asNumber.HasValue)
						{
							switch (op)
							{
								case "+":
									value += asNumber.Value;
									break;
								case "-":
									value -= asNumber.Value;
									break;
								case "*":
									value *= asNumber.Value;
									break;
								case "/":
									if (asNumber.Value == 0)
									{
										LogError(Errors.DivideByZero, expression);
									}
									else
									{
										value /= asNumber.Value;
									}
									break;
								case "%":
									if (asNumber.Value == 0)
									{
										LogError(Errors.DivideByZero, expression);
									}
									else
									{
										value %= asNumber.Value;
									}
									break;
							}
						}
						else
						{
							evaluated = false;
						}
					}
				}
			}
			else
			{
				evaluated = false;
			}

			return (evaluated, value);
		}

		/// <summary>
		/// Generate a sorted list of all valid instructions.
		/// </summary>
		/// <remarks>
		/// This is done purely so that a binary search can be performed when trying to find instruction details.
		/// </remarks>
		private void GenerateAllInstructionList()
		{
			List<(OpcodePrefix prefix, Dictionary<byte, InstructionInfo> dctInstrInfo)> tables = new List<(OpcodePrefix, Dictionary<byte, InstructionInfo>)>
			{
				(OpcodePrefix.None, InstructionTables.Unprefixed_Instructions),
				(OpcodePrefix.CB, InstructionTables.CBPrefixed_Instructions),
				(OpcodePrefix.ED, InstructionTables.EDPrefixed_Instructions),
				(OpcodePrefix.DD, InstructionTables.DDPrefixed_Instructions),
				(OpcodePrefix.FD, InstructionTables.FDPrefixed_Instructions),
				(OpcodePrefix.DDCB, InstructionTables.DDCBPrefixed_Instructions),
				(OpcodePrefix.FDCB, InstructionTables.FDCBPrefixed_Instructions)
			};

			foreach (var (prefix, dctInstrInfo) in tables)
			{
				foreach (var inst in dctInstrInfo)
				{
					_allInstructions.Add((inst.Value.Mnemonic, prefix, inst.Key));
				}
			}

			// Sort entire list by the instruction string (so that they are in the alphabetical order
			// required for a binary search).
			_allInstructions = _allInstructions.OrderBy(x => x.instruction).ToList();
		}

		/// <summary>
		/// Generate a list of reserved words (i.e. Z80 instructions, assembler directives, register names, etc.).
		/// </summary>
		/// <remarks>
		/// This list is used to check that these reserved words are not used for label names, EQUs, etc.)
		/// </remarks>
		private void GenerateReservedWordList()
		{
			foreach (var (instruction, _, _) in _allInstructions)
			{
				string word = instruction;
				int spacePos = word.IndexOf(' ');
				if (spacePos >= 0)
				{
					word = word.Substring(0, spacePos);
				}
				if (!_reservedWords.Contains(word))
				{
					_reservedWords.Add(word);
				}
			}
			foreach (var reg in _validRegs)
			{
				_reservedWords.Add(reg);
			}
			foreach (var flag in _validFlags)
			{
				_reservedWords.Add(flag);
			}
			_reservedWords.Add("ORG");
			_reservedWords.Add("DB");
			_reservedWords.Add("DEFB");
			_reservedWords.Add("DW");
			_reservedWords.Add("DEFW");
			_reservedWords.Add("DS");
			_reservedWords.Add("DEFS");
			_reservedWords.Add("DM");
			_reservedWords.Add("DEFM");
			_reservedWords.Add("EQU");
			_reservedWords.Add("$");
		}
	}
}

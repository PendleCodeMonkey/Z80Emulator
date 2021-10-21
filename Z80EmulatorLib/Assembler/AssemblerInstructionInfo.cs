using System.Collections.Generic;
using static PendleCodeMonkey.Z80EmulatorLib.Assembler.AssemblerEnumerations;

namespace PendleCodeMonkey.Z80EmulatorLib.Assembler
{
	/// <summary>
	/// Implementation of the <see cref="AssemblerInstructionInfo"/> class.
	/// </summary>
	class AssemblerInstructionInfo
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AssemblerInstructionInfo"/> class.
		/// </summary>
		/// <param name="lineNumber">The line number in the source code corresponding to this instruction.</param>
		/// <param name="address">The address of this instruction.</param>
		/// <param name="instruction">The <see cref="Instruction"/> object corresponding to this instruction.</param>
		/// <param name="operand1">The first operand.</param>
		/// <param name="operand1Type">Enumerated operand type for the first operand.</param>
		/// <param name="operand2">The second operand.</param>
		/// <param name="operand2Type">Enumerated operand type for the second operand.</param>
		/// <param name="dataSegment">An instance of the <see cref="DataSegmentInfo"/> class (or null if
		///								this <see cref="AssemblerInstructionInfo"/> instance does not correspond to a data segment).</param>
		internal AssemblerInstructionInfo(int lineNumber, ushort address, Instruction instruction, string operand1, OperandType operand1Type, string operand2, OperandType operand2Type, DataSegmentInfo dataSegment = null)
		{
			LineNumber = lineNumber;
			Address = address;
			Instruction = instruction;
			Operand1 = operand1;
			Operand1Type = operand1Type;
			Operand2 = operand2;
			Operand2Type = operand2Type;
			DataSegment = dataSegment;
			BinaryData = null;
		}

		/// <summary>
		/// Gets or sets the line number in the source code corresponding to this instruction.
		/// </summary>
		internal int LineNumber { get; set; }

		/// <summary>
		/// Gets or sets the address of this instruction.
		/// </summary>
		internal ushort Address { get; set; }

		/// <summary>
		/// Gets or sets the <see cref="Instruction"/> object corresponding to this instruction.
		/// </summary>
		internal Instruction Instruction { get; set; }

		/// <summary>
		/// Gets or sets the first operand.
		/// </summary>
		internal string Operand1 { get; set; }

		/// <summary>
		/// Gets or sets the enumerated operand type for the first operand.
		/// </summary>
		internal OperandType Operand1Type { get; set; }

		/// <summary>
		/// Gets or sets the second operand.
		/// </summary>
		internal string Operand2 { get; set; }

		/// <summary>
		/// Gets or sets the enumerated operand type for the second operand.
		/// </summary>
		internal OperandType Operand2Type { get; set; }

		/// <summary>
		/// Gets or sets the object used for storing information related to a data segment.
		/// </summary>
		internal DataSegmentInfo DataSegment { get; set; }

		/// <summary>
		/// Gets or sets the list of binary data generated for this <see cref="AssemblerInstructionInfo"/> instance.
		/// </summary>
		internal List<byte> BinaryData { get; set; }


		/// <summary>
		/// Generate the binary data for this <see cref="AssemblerInstructionInfo"/> instance.
		/// </summary>
		/// <returns>The generated binary data as a collection of bytes.</returns>
		internal List<byte> GenerateBinaryData()
		{
			List<byte> binData = new List<byte>();

			// If this object corresponds to a data segment then just retrieve the binary data for it from
			// the DataSegmentInfo object.
			if (DataSegment != null)
			{
				binData = DataSegment.Data;
			}
			else
			{
				// This instance doesn't correspond to a data segment so handle it as a Z80 instruction.

				// Add the prefix bytes required for this instruction (if any)
				switch (Instruction.Prefix)
				{
					case Enumerations.OpcodePrefix.CB:
						binData.Add(0xCB);
						break;
					case Enumerations.OpcodePrefix.DD:
						binData.Add(0xDD);
						break;
					case Enumerations.OpcodePrefix.ED:
						binData.Add(0xED);
						break;
					case Enumerations.OpcodePrefix.FD:
						binData.Add(0xFD);
						break;
					case Enumerations.OpcodePrefix.DDCB:
						binData.Add(0xDD);
						binData.Add(0xCB);
						break;
					case Enumerations.OpcodePrefix.FDCB:
						binData.Add(0xFD);
						binData.Add(0xCB);
						break;
					default:
						// No prefix bytes so nothing need adding to the binary data here.
						break;
				}

				// Now add the opcode and operand bytes.
				if (Instruction.Prefix != Enumerations.OpcodePrefix.DDCB && Instruction.Prefix != Enumerations.OpcodePrefix.FDCB)
				{
					binData.Add(Instruction.Opcode);
				}
				if (Instruction.Info.AddrMode1 == Enumerations.AddrMode.Relative || Instruction.Info.AddrMode2 == Enumerations.AddrMode.Relative)
				{
					binData.Add(Instruction.Displacement);
				}
				if (Instruction.Info.AddrMode1 == Enumerations.AddrMode.Immediate || Instruction.Info.AddrMode2 == Enumerations.AddrMode.Immediate)
				{
					binData.Add(Instruction.ByteOperand);
				}
				if (Instruction.Info.AddrMode1 == Enumerations.AddrMode.ExtendedImmediate || Instruction.Info.AddrMode2 == Enumerations.AddrMode.ExtendedImmediate)
				{
					binData.Add((byte)(Instruction.WordOperand & 0xFF));
					binData.Add((byte)(Instruction.WordOperand >> 8 & 0xFF));
				}
				if (Instruction.Prefix == Enumerations.OpcodePrefix.DDCB || Instruction.Prefix == Enumerations.OpcodePrefix.FDCB)
				{
					binData.Add(Instruction.Opcode);
				}
			}

			BinaryData = new List<byte>(binData);

			return binData;
		}
	}
}

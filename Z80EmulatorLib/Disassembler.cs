using PendleCodeMonkey.Z80EmulatorLib.Enumerations;
using System;
using System.Collections.Generic;
using System.Text;

namespace PendleCodeMonkey.Z80EmulatorLib
{
	/// <summary>
	/// Implementation of the <see cref="Disassembler"/> class.
	/// </summary>
	public class Disassembler
	{
		private const int MaxNonExecDataBlockSize = 16;
		private readonly List<(ushort Address, ushort Length)> _nonExecutableSections = new List<(ushort, ushort)>();

		/// <summary>
		/// Initializes a new instance of the <see cref="Disassembler"/> class.
		/// </summary>
		/// <param name="machine">The <see cref="Machine"/> instance for which this object is handling the disassembly of instructions.</param>
		/// <param name="startAddress">The start address of the block of memory being disassembled.</param>
		/// <param name="length">The length (in bytes) of the block of memory being disassembled.</param>
		public Disassembler(Machine machine, ushort startAddress, ushort length)
		{
			Machine = machine ?? throw new ArgumentNullException(nameof(machine));
			StartAddress = startAddress;
			Length = length;
			CurrentAddress = StartAddress;
		}

		/// <summary>
		/// Gets or sets the <see cref="Machine"/> instance for which this <see cref="Disassembler"/> instance
		/// is handling the disassembly of instructions
		/// </summary>
		private Machine Machine { get; set; }

		/// <summary>
		/// Gets or sets the start address of the block of memory being disassembled.
		/// </summary>
		private ushort StartAddress { get; set; }

		/// <summary>
		/// Gets or sets the length of the block of memory being disassembled.
		/// </summary>
		private ushort Length { get; set; }

		/// <summary>
		/// Gets or sets the address of the current byte in the block of memory being disassembled.
		/// </summary>
		internal ushort CurrentAddress { get; set; }

		/// <summary>
		/// Gets a value indicating if the disassembly has reached the end of the specified block of memory.
		/// </summary>
		internal bool IsEndOfData => CurrentAddress >= StartAddress + Length;

		/// <summary>
		/// Gets the list of non-executable sections (i.e. blocks of memory that the disassembler treats as non-executable)
		/// </summary>
		public List<(ushort Address, ushort Length)> NonExecutableSections => _nonExecutableSections;

		/// <summary>
		/// Add details of a non-executable block of data.
		/// </summary>
		/// <remarks>
		/// Non-executable sections are blocks of memory that contain data that is not executable code.
		/// Such data blocks are shown in the disassembly output using a DB directive.
		/// </remarks>
		/// <param name="startAddress">The start address of the block of non-executable data.</param>
		/// <param name="length">The length (in bytes) of the block of non-executable data.</param>
		public void AddNonExecutableSection(ushort startAddress, ushort length)
		{
			_nonExecutableSections.Add((startAddress, length));
		}

		/// <summary>
		/// Remove the record of a specific non-executable block of data.
		/// </summary>
		/// <remarks>
		/// Note that this does not actually remove the data itself, it just stops the disassembler treating
		/// that block of data as non-executable.
		/// </remarks>
		/// <param name="sectionIndex">Zero-based index of the non-executable block to be removed.</param>
		/// <returns><c>true</c> if the record of the non-executabe block was removed, otherwise <c>false</c>.</returns>
		public bool RemoveNonExecutableSection(int sectionIndex)
		{
			if (sectionIndex >= 0 && sectionIndex < _nonExecutableSections.Count)
			{
				_nonExecutableSections.RemoveAt(sectionIndex);
				return true;
			}

			return false;
		}

		/// <summary>
		/// Perform a full disassembly of the specified block of memory.
		/// </summary>
		/// <returns>A list of tuples/ Each tuple contains:
		///   1. The memory address of the instruction.
		///   2. A string containing the disassembled version of the instruction/data at that address.
		/// </returns>
		public List<(ushort Address, string Disassembly)> Disassemble()
		{
			List<(ushort, string)> result = new List<(ushort, string)>();

			// Cache the current Program Counter
			ushort savedPC = Machine.CPU.PC;

			// Set Program Counter to specified disassembly start address (this is required because the method that
			// fetches the next instruction works with the Program Counter).
			Machine.CPU.PC = StartAddress;
			CurrentAddress = StartAddress;
			while (!IsEndOfData)
			{
				var nonExecSection = WithinNonExecutableSection();
				if (nonExecSection >= 0)
				{
					result.Add((CurrentAddress, NonExecutableData(nonExecSection)));
				}
				else
				{
					result.Add((CurrentAddress, DisassembleInstruction()));
				}
				CurrentAddress = Machine.CPU.PC;
			}

			// Restore the cached Program Counter value.
			Machine.CPU.PC = savedPC;

			return result;
		}

		/// <summary>
		/// Disassemble the instruction located at the current address in memory.
		/// </summary>
		/// <returns>A string containing the disassembled version of the instruction at the current address.</returns>
		private string DisassembleInstruction()
		{
			// Fetch the instruction at the current Program Counter address.
			var instruction = Machine.Decoder.FetchInstruction();

			string opCode = instruction.Info.Mnemonic;
			if (instruction.Info.AddrMode1 == AddrMode.Immediate || instruction.Info.AddrMode2 == AddrMode.Immediate)
			{
				opCode = opCode.Replace("n", $"{instruction.ByteOperand:X2}h");
			}
			if (instruction.Info.AddrMode1 == AddrMode.Relative || instruction.Info.AddrMode2 == AddrMode.Relative)
			{
				var d = (sbyte)instruction.Displacement;
				if (opCode.Contains('e'))
				{
					ushort address = (ushort)(Machine.CPU.PC + d);
					opCode = opCode.Replace("e", $"{address:X4}h");
				}
				else
				{
					opCode = opCode.Replace("+d", $"{d:+0;-#}");
				}
			}
			if (instruction.Info.AddrMode1 == AddrMode.ExtendedImmediate || instruction.Info.AddrMode2 == AddrMode.ExtendedImmediate)
			{
				opCode = opCode.Replace("nn", $"{instruction.WordOperand:X4}h");
			}

			// (IX+0) and (IY+0) are output as just (IX) and (IY) respectively.
			opCode = opCode.Replace("(IX+0)", "(IX)");
			opCode = opCode.Replace("(IY+0)", "(IY)");

			return opCode;
		}

		/// <summary>
		/// Determines if the current address is within a non-executable data block.
		/// </summary>
		/// <returns>The zero-based index of the non-executable data block that the current address falls within, or -1 if
		/// the current address is within executable code.</returns>
		private int WithinNonExecutableSection()
		{
			foreach (var nonExec in _nonExecutableSections)
			{
				if (CurrentAddress >= nonExec.Address && CurrentAddress < (nonExec.Address + nonExec.Length))
				{
					return _nonExecutableSections.IndexOf(nonExec);
				}
			}

			return -1;
		}

		/// <summary>
		/// Returns a string containing a block of non-executable data.
		/// </summary>
		/// <remarks>
		/// Non-executable data it output in the disassembly using a DB directive.
		/// Non-executable data sections are output in blocks of a maximum of 16 bytes (as set by MaxNonExecDataBlockSize).
		/// </remarks>
		/// <param name="nonExecSection">Zero-based index of the non-executable section.</param>
		/// <returns>A string containing the disassembled output for the block of non-executable data.</returns>
		private string NonExecutableData(int nonExecSection)
		{
			StringBuilder sb = new StringBuilder();
			var section = _nonExecutableSections[nonExecSection];

			sb.Append("DB ");

			int bytesRemaining = section.Address + section.Length - CurrentAddress;
			for (int i = 0; i < Math.Min(MaxNonExecDataBlockSize, bytesRemaining); i++)
			{
				byte value = Machine.Decoder.ReadNextPCByte();
				if (i > 0)
				{
					sb.Append(", ");
				}
				sb.Append($"{value:X2}h");
			}
			return sb.ToString();
		}

	}
}

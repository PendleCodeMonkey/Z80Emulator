using PendleCodeMonkey.Z80EmulatorLib.Enumerations;
using System;
using System.Collections.Generic;

namespace PendleCodeMonkey.Z80EmulatorLib
{
	/// <summary>
	/// Implementation of the <see cref="InstructionDecoder"/> class.
	/// </summary>
	class InstructionDecoder
	{

		/// <summary>
		/// Initializes a new instance of the <see cref="InstructionDecoder"/> class.
		/// </summary>
		/// <param name="machine">The <see cref="Machine"/> instance for which this object is handling the execution of instructions.</param>
		internal InstructionDecoder(Machine machine)
		{
			Machine = machine ?? throw new ArgumentNullException(nameof(machine));
		}

		/// <summary>
		/// Gets or sets the <see cref="Machine"/> instance for which this <see cref="InstructionDecoder"/> instance
		/// is handling the decoding of instructions.
		/// </summary>
		private Machine Machine { get; set; }

		/// <summary>
		/// Return the byte located at the Program Counter, and then increment the Program Counter.
		/// </summary>
		/// <returns>The byte located at the Program Counter.</returns>
		internal byte ReadNextPCByte()
		{
			if (Machine.IsEndOfData)
			{
				throw new InvalidOperationException("Execution has run past the end of the loaded data.");
			}
			byte value = Machine.Memory.Read(Machine.CPU.PC);
			Machine.CPU.IncrementPC();
			return value;
		}

		/// <summary>
		/// Fetch the instruction located at the current Program Counter address, incrementing the
		/// Program Counter accordingly.
		/// </summary>
		/// <returns>An <see cref="Instruction"/> instance containing details about the instruction that has been fetched.</returns>
		internal Instruction FetchInstruction()
		{
			byte byteOperand = 0;
			ushort wordOperand = 0;
			byte displacement = 0;
			OpcodePrefix prefix = OpcodePrefix.None;

			var value = ReadNextPCByte();

			Dictionary<byte, InstructionInfo> table;
			if (value == 0xCB)
			{
				value = ReadNextPCByte();
				table = InstructionTables.CBPrefixed_Instructions;
				prefix = OpcodePrefix.CB;
			}
			else if (value == 0xDD)
			{
				value = ReadNextPCByte();
				if (value == 0xCB)
				{
					displacement = ReadNextPCByte();		// Displacement comes before opcode for DDCB prefixed instructions.
					value = ReadNextPCByte();
					table = InstructionTables.DDCBPrefixed_Instructions;
					prefix = OpcodePrefix.DDCB;
				}
				else
				{
					table = InstructionTables.DDPrefixed_Instructions;
					prefix = OpcodePrefix.DD;
				}
			}
			else if (value == 0xED)
			{
				value = ReadNextPCByte();
				table = InstructionTables.EDPrefixed_Instructions;
				prefix = OpcodePrefix.ED;
			}
			else if (value == 0xFD)
			{
				value = ReadNextPCByte();
				if (value == 0xCB)
				{
					displacement = ReadNextPCByte();        // Displacement comes before opcode for FDCB prefixed instructions.
					value = ReadNextPCByte();
					table = InstructionTables.FDCBPrefixed_Instructions;
					prefix = OpcodePrefix.FDCB;
				}
				else
				{
					table = InstructionTables.FDPrefixed_Instructions;
					prefix = OpcodePrefix.FD;
				}
			}
			else
			{
				table = InstructionTables.Unprefixed_Instructions;
			}
			if (table.ContainsKey(value))
			{
				InstructionInfo info = table[value];

				// If Relative addressing mode the read the displacement byte (but not if this is a DDCB or FDCB prefixed instruction
				// because the displacement has already been retrieved for those)
				if (info.AddrMode1 == AddrMode.Relative && prefix != OpcodePrefix.DDCB && prefix != OpcodePrefix.FDCB)
				{
					displacement = ReadNextPCByte();
				}
				if (info.AddrMode1 == AddrMode.Immediate || info.AddrMode2 == AddrMode.Immediate)
				{
					byteOperand = ReadNextPCByte();
				}
				if (info.AddrMode1 == AddrMode.ExtendedImmediate || info.AddrMode2 == AddrMode.ExtendedImmediate)
				{
					byte lo = ReadNextPCByte();
					byte hi = ReadNextPCByte();
					wordOperand = (ushort)(hi << 8 | lo);
				}
				Instruction inst = new Instruction(value, prefix, info, byteOperand, wordOperand, displacement);
				return inst;
			}

			return null;
		}
	}
}

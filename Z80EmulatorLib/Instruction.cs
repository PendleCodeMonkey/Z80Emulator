using PendleCodeMonkey.Z80EmulatorLib.Enumerations;

namespace PendleCodeMonkey.Z80EmulatorLib
{
	/// <summary>
	/// Implementation of the <see cref="Instruction"/> class.
	/// </summary>
	class Instruction
	{

		/// <summary>
		/// Initializes a new instance of the <see cref="Instruction"/> class.
		/// </summary>
		/// <param name="opcode">The 8-bit opcode value for the instruction.</param>
		/// <param name="prefix">The enumerated prefix value for the instruction.</param>
		/// <param name="info">An <see cref="InstructionInfo"/> instance giving info about the instruction.</param>
		/// <param name="byteOperand">The value of the 8-bit operand (if any).</param>
		/// <param name="wordOperand">The value of the 16-bit operand (if any).</param>
		/// <param name="displacement">The value of the 8-bit displacement (if any).</param>
		internal Instruction(byte opcode, OpcodePrefix prefix, InstructionInfo info, byte byteOperand, ushort wordOperand, byte displacement)
		{
			Opcode = opcode;
			Prefix = prefix;
			Info = info;
			ByteOperand = byteOperand;
			WordOperand = wordOperand;
			Displacement = displacement;
		}


		/// <summary>
		/// The 8-bit opcode value for this instruction.
		/// </summary>
		/// <remarks>
		/// This is only the 8-bit opcode value itself, it does not include any prefix bytes used for this instruction.
		/// </remarks>
		internal byte Opcode { get; set; }

		/// <summary>
		/// The enumerated prefix value for the instruction (e.g. None, CB, ED, DD, FD, DDCB, FDCB)
		/// </summary>
		internal OpcodePrefix Prefix { get; set; }

		/// <summary>
		/// An <see cref="InstructionInfo"/> instance giving info about the instruction.
		/// </summary>
		internal InstructionInfo Info { get; set; }

		/// <summary>
		/// The value of the 8-bit operand specified for this instruction (if any).
		/// </summary>
		internal byte ByteOperand { get; set; }

		/// <summary>
		/// The value of the 16-bit operand specified for this instruction (if any).
		/// </summary>
		internal ushort WordOperand { get; set; }

		/// <summary>
		/// The value of the 8-bit displacement specified for this instruction (if any).
		/// </summary>
		internal byte Displacement { get; set; }

	}
}

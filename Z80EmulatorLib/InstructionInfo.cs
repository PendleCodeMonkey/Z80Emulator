using PendleCodeMonkey.Z80EmulatorLib.Enumerations;

namespace PendleCodeMonkey.Z80EmulatorLib
{
	/// <summary>
	/// Implementation of the <see cref="InstructionInfo"/> class.
	/// </summary>
	class InstructionInfo
	{

		/// <summary>
		/// Initializes a new instance of the <see cref="InstructionInfo"/> class.
		/// </summary>
		/// <param name="mnemonic">The mnemonic for the instruction.</param>
		/// <param name="handlerID">Enumerated ID that determines the method that will be used to handle the instruction.</param>
		/// <param name="addrMode1">Enumerated value for the first addressing mode used for the instruction.</param>
		/// <param name="addrMode2">Enumerated value for the second addressing mode used for the instruction.</param>
		internal InstructionInfo(string mnemonic, OpHandlerID handlerID, AddrMode addrMode1, AddrMode addrMode2)
		{
			Mnemonic = mnemonic;
			HandlerID = handlerID;
			AddrMode1 = addrMode1;
			AddrMode2 = addrMode2;
		}

		/// <summary>
		/// The mnemonic for this instruction.
		/// </summary>
		internal string Mnemonic { get; set; }

		/// <summary>
		/// Enumerated ID that determines the method that will be used to handle this instruction.
		/// </summary>
		internal OpHandlerID HandlerID { get; set; }

		/// <summary>
		/// Enumerated value for the first addressing mode used for this instruction.
		/// </summary>
		internal AddrMode AddrMode1 { get; set; }

		/// <summary>
		/// Enumerated value for the second addressing mode used for this instruction.
		/// </summary>
		internal AddrMode AddrMode2 { get; set; }
	}
}

using System.Collections.Generic;
using static PendleCodeMonkey.Z80EmulatorLib.Assembler.AssemblerEnumerations;

namespace PendleCodeMonkey.Z80EmulatorLib.Assembler
{
	/// <summary>
	/// Implementation of the <see cref="DataSegmentInfo"/> class.
	/// </summary>
	class DataSegmentInfo
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DataSegmentInfo"/> class.
		/// </summary>
		/// <param name="type">Enumerated type of the data segment.</param>
		/// <param name="tokens">Collection of token strings corresponding to the data segment.</param>
		/// <param name="data">Binary data for the data segment as a collection of bytes.</param>
		internal DataSegmentInfo(DataSegmentType type, List<string> tokens, List<byte> data)
		{
			Type = type;
			Tokens = tokens;
			Data = data;
		}

		/// <summary>
		/// Gets or sets the binary data for the data segment as a collection of bytes
		/// </summary>
		internal List<byte> Data { get; set; }

		/// <summary>
		/// Gets or sets the enumerated type of the data segment.
		/// </summary>
		internal DataSegmentType Type { get; set; }

		/// <summary>
		/// Gets or sets the collection of token strings corresponding to the data segment.
		/// </summary>
		internal List<string> Tokens { get; set; }
	}
}

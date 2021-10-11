using System.Collections.Generic;

namespace PendleCodeMonkey.Z80EmulatorLib
{
	/// <summary>
	/// Implementation of the <see cref="DummyPort"/> class.
	/// This class provides a dummy implementation of an IPort that is used
	/// by the IN/OUT instructions when no 'proper' port has been specified.
	/// </summary>
	class DummyPort : IPort
	{
		internal List<byte> DummyData { get; set; }


		public DummyPort()
		{
			DummyData = new List<byte>();

			// Add some dummy port data.
			for (int i = 0; i < 256; i++)
			{
				DummyData.Add((byte)(255 - i));
			}
		}

		/// <summary>
		/// Read a byte of data from the specified port.
		/// </summary>
		/// <param name="port">The port from which data should be read.</param>
		/// <returns>The byte of data read from the specified port.</returns>
		public byte Read(ushort port)
		{
			return DummyData[port & 0x00FF];
		}

		/// <summary>
		/// Write a byte of data to the specified port.
		/// </summary>
		/// <param name="port">The port to which data should be written.</param>
		/// <param name="data">The byte of data to be written to the specified port.</param>
		public void Write(ushort port, byte data)
		{
			DummyData[port & 0x00FF] = data;
		}

	}
}

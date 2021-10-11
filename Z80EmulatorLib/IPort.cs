using System;
using System.Collections.Generic;
using System.Text;

namespace PendleCodeMonkey.Z80EmulatorLib
{
	public interface IPort
	{
		/// <summary>
		/// Read a byte of data from the specified port.
		/// </summary>
		/// <param name="port">The port from which data should be read.</param>
		/// <returns>The byte of data read from the specified port.</returns>
		byte Read(ushort port);

		/// <summary>
		/// Write a byte of data to the specified port.
		/// </summary>
		/// <param name="port">The port to which data should be written.</param>
		/// <param name="data">The byte of data to be written to the specified port.</param>
		void Write(ushort port, byte data);
	}
}

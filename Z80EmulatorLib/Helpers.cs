namespace PendleCodeMonkey.Z80EmulatorLib
{
	/// <summary>
	/// Implementation of the <see cref="Helpers"/> static class.
	/// </summary>
	public static class Helpers
	{
		/// <summary>
		/// Calculate the parity of the specified byte value.
		/// </summary>
		/// <param name="value">The value for which parity is to be calculated.</param>
		/// <returns><c>true</c> if the value has even parity, otherwise <c>false</c>.</returns>
		public static bool Parity(byte value)
		{
			value ^= (byte)(value >> 4);
			value &= 0x0f;
			return ((0x6996 >> value) & 0x01) == 0x00;
		}
	}
}

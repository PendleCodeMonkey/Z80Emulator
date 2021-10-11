using PendleCodeMonkey.Z80EmulatorLib;
using Xunit;

namespace PendleCodeMonkey.Z80Emulator.Tests
{
	public class HelpersTests
	{
		[Theory]
		[InlineData(0x00, true)]
		[InlineData(0x01, false)]
		[InlineData(0x02, false)]
		[InlineData(0x03, true)]
		[InlineData(0x04, false)]
		[InlineData(0x05, true)]
		[InlineData(0x06, true)]
		[InlineData(0x07, false)]
		[InlineData(0x77, true)]
		[InlineData(0x80, false)]
		public void ParityTest(byte value, bool expectedResult)
		{
			bool result = Helpers.Parity(value);

			// Assert
			Assert.Equal(expectedResult, result);
		}
	}
}

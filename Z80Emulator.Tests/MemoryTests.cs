using PendleCodeMonkey.Z80EmulatorLib;
using System;
using System.Linq;
using Xunit;

namespace PendleCodeMonkey.Z80Emulator.Tests
{
	public class MemoryTests
	{
		[Fact]
		public void NewMemory_ShouldNotBeNull()
		{
			Memory memory = new Memory();

			Assert.NotNull(memory);
		}

		[Fact]
		public void NewMemory_ShouldBeCorrectlyAllocated()
		{
			Memory memory = new Memory();

			Assert.NotNull(memory.Data);
			Assert.Equal(0x10000, memory.Data.Length);
		}

		[Fact]
		public void LoadData_SucceedsWhenDataFitsInMemory()
		{
			Memory memory = new Memory();
			var success = memory.LoadData(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x0 }, 0x2000);

			Assert.True(success);
		}

		[Fact]
		public void LoadData_FailsWhenDataExceedsMemoryLimit()
		{
			Memory memory = new Memory();
			var success = memory.LoadData(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x0 }, 0xFFFC);

			Assert.False(success);
		}

		[Fact]
		public void Clear_ShouldClearAllMemory()
		{
			Memory memory = new Memory();
			Span<byte> machineMemorySpan = memory.Data;
			machineMemorySpan.Fill(0xAA);

			memory.Clear();

			Assert.False(memory.Data.Where(x => x > 0).Any());
		}

		[Fact]
		public void DumpMemory_ShouldReturnRequestedMemoryBlock()
		{
			Memory memory = new Memory();
			_ = memory.LoadData(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 }, 0x2000);

			var dump = memory.DumpMemory(0x2001, 0x0004);

			Assert.Equal(4, dump.Length);
			Assert.Equal(0x02, dump[0]);
			Assert.Equal(0x03, dump[1]);
			Assert.Equal(0x04, dump[2]);
			Assert.Equal(0x05, dump[3]);
		}

		[Fact]
		public void Read_ShouldReturnCorrectData()
		{
			Memory memory = new Memory();
			var _ = memory.LoadData(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x0 }, 0x2000);

			byte value = memory.Read(0x2002);

			Assert.Equal(0x03, value);
		}

		[Fact]
		public void Read16bit_ShouldReturnCorrectData()
		{
			Memory memory = new Memory();
			var _ = memory.LoadData(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x0 }, 0x2000);

			ushort value = memory.Read16bit(0x2002);

			Assert.Equal(0x0403, value);
		}

		[Fact]
		public void Write_ShouldWriteCorrectData()
		{
			Memory memory = new Memory();
			var _ = memory.LoadData(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x0 }, 0x2000);

			memory.Write(0x2002, 0x20);

			Assert.Equal(0x20, memory.Data[0x2002]);
		}

		[Fact]
		public void Write16bit_ShouldWriteCorrectData()
		{
			Memory memory = new Memory();
			var _ = memory.LoadData(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x0 }, 0x2000);

			memory.Write16bit(0x2002, 0x3040);

			Assert.Equal(0x40, memory.Data[0x2002]);
			Assert.Equal(0x30, memory.Data[0x2003]);
		}

	}
}

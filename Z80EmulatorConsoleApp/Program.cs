using PendleCodeMonkey.Z80EmulatorLib;
using System;

namespace Z80EmulatorConsoleApp
{
	class Program
	{
		static void Main(string[] _)
		{

			MemCpy();

			TestDisassembler();

			DivisionTest();
		}

		static void MemCpy()
		{
			Console.WriteLine("MemCpy example:");
			Console.WriteLine("---------------");

			Machine machine = new Machine();

			CPUState state = new CPUState
			{
				DE = 0x2000,			// Source
				HL = 0x3000,			// Destination
				BC = 0x0010,			// Num bytes
				SP = 0x4000
			};

			// Code to copy BC bytes from address in DE to address in HL (i.e. copy 0x0010 bytes fromm 0x2000 to 0x3000)
			byte[] code = new byte[] { 0x78, 0xB1, 0xC8, 0x1A, 0x77, 0x13, 0x23, 0x0B, 0xC3, 0x00, 0x10 };
			machine.LoadExecutableData(code, 0x1000);
			machine.SetCPUState(state);

			// Load some data into 0x2000
			byte[] data = new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA };
			machine.LoadData(data, 0x2000, false);

			machine.Execute();

			// Dump out the contents of memory address 0x3000 - 0x300A (to show the code has worked)
			Console.WriteLine();
			Console.WriteLine("Memory dump:");
			var mem = machine.DumpMemory(0x3000, 0x000A);
			for (int n = 0; n < 0x0A; n++)
			{
				Console.Write($"{mem[n]}  ");
			}

			Console.WriteLine();

			Console.WriteLine();
			Console.WriteLine("CPU state dump:");
			string dump = machine.Dump();
			Console.WriteLine(dump);

		}

		static void TestDisassembler()
		{
			Console.WriteLine();
			Console.WriteLine("Disassembler example:");
			Console.WriteLine("---------------------");
			Console.WriteLine();

			Machine machine = new Machine();

			byte[] code = new byte[] { 0x37, 0x3F, 0xDD, 0x7E, 0x00, 0xFD, 0x8E, 0x00, 0x77, 0xDD, 0x2B, 0xFD, 0x2B, 0x2B, 0x10, 0xF2, 0xC9,
										0x11, 0x22, 0x22, 0x33, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA,
										0x21, 0x00, 0x00, 0x3E, 0x10, 0xCB, 0x21, 0xCB, 0x10, 0xED, 0x6A, 0xED,
										0x52, 0x38, 0x03, 0x0C, 0x18, 0x01, 0x19, 0x3D, 0x20, 0xEF, 0xC9};
			machine.LoadExecutableData(code, 0x1000);

			Disassembler disassembler = new Disassembler(machine, startAddress: 0x1000, length: (ushort)code.Length);
			disassembler.AddNonExecutableSection(0x1011, 0x000A);
			var dis = disassembler.Disassemble();

			foreach (var (Address, Disassembly) in dis)
			{
				Console.WriteLine($"0x{Address:X4} - {Disassembly}");
			}

		}


		static void DivisionTest()
		{
			Console.WriteLine();
			Console.WriteLine("16-bit division example:");
			Console.WriteLine("------------------------");
			Machine machine = new Machine();

			CPUState state = new CPUState
			{
				BC = 0x3264,
				DE = 0x001B,
				SP = 0x4000
			};

			// Code that divides BC by DE; giving result in BC and remainder in HL
			byte[] code = new byte[] { 0x21, 0x00, 0x00, 0x3E, 0x10, 0xCB, 0x21, 0xCB, 0x10, 0xED, 0x6A, 0xED,
										0x52, 0x38, 0x03, 0x0C, 0x18, 0x01, 0x19, 0x3D, 0x20, 0xEF, 0xC9 };
			machine.LoadExecutableData(code, 0x0000);
			machine.SetCPUState(state);

			machine.Execute();

			Console.WriteLine();
			Console.WriteLine("CPU state dump:");

			string dump = machine.Dump();
			Console.WriteLine(dump);

		}
	}
}

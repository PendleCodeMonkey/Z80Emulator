using System;

namespace PendleCodeMonkey.Z80EmulatorLib
{
	/// <summary>
	/// Implementation of the <see cref="Stack"/> class.
	/// </summary>
	class Stack
	{
		private readonly Memory _memory;
		private readonly CPU _cpu;

		/// <summary>
		/// Initializes a new instance of the <see cref="Stack"/> class.
		/// </summary>
		/// <param name="cpu">The <see cref="CPU"/> instance for which this stack is being used.</param>
		/// <param name="memory">The <see cref="Memory"/> instance that will host the memory used for the stack.</param>
		internal Stack(CPU cpu, Memory memory)
		{
			_cpu = cpu ?? throw new ArgumentNullException(nameof(cpu));
			_memory = memory ?? throw new ArgumentNullException(nameof(memory));
		}

		/// <summary>
		/// Push the specified 16-bit value onto the stack.
		/// </summary>
		/// <param name="value">The 16-bit value to be pushed onto the stack.</param>
		internal void Push(ushort value)
		{
			_memory.Write(--_cpu.SP, (byte)(value >> 8));		// High byte
			_memory.Write(--_cpu.SP, (byte)value);				// Low byte
		}

		/// <summary>
		/// Pop a 16-bit value off the top of the stack.
		/// </summary>
		/// <returns>The 16-bit value retrieved from the top of the stack.</returns>
		internal ushort Pop()
		{
			byte lo = _memory.Read(_cpu.SP++);
			byte hi = _memory.Read(_cpu.SP++);
			return (ushort)(hi << 8 | lo);
		}
	}
}

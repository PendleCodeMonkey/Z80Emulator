using PendleCodeMonkey.Z80EmulatorLib.Enumerations;
using System;
using System.Collections.Generic;
using System.Text;

namespace PendleCodeMonkey.Z80EmulatorLib
{
	/// <summary>
	/// Implementation of the <see cref="CPUState"/> class.
	/// </summary>
	/// <remarks>
	/// This class holds information that can be used to get or set individual elements
	/// of the CPU state. The number of CPU state elements is quite large and therefore it
	/// is preferable to have a single object for handling this state info rather than having
	/// a method (a constructor, for example) with a dozen or more arguments.
	/// </remarks>
	public class CPUState
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CPUState"/> class.
		/// </summary>
		public CPUState()
		{
		}

		/// <summary>
		/// Gets or sets the value of the AF register pair.
		/// </summary>
		public ushort? AF { get; set; }

		/// <summary>
		/// Gets or sets the value of the BC register pair.
		/// </summary>
		public ushort? BC { get; set; }

		/// <summary>
		/// Gets or sets the value of the DE register pair.
		/// </summary>
		public ushort? DE { get; set; }

		/// <summary>
		/// Gets or sets the value of the HL register pair.
		/// </summary>
		public ushort? HL { get; set; }

		/// <summary>
		/// Gets or sets the value of the IX register pair.
		/// </summary>
		public ushort? IX { get; set; }

		/// <summary>
		/// Gets or sets the value of the IY register pair.
		/// </summary>
		public ushort? IY { get; set; }

		/// <summary>
		/// Gets or sets the value of the Program Counter.
		/// </summary>
		public ushort? PC { get; set; }

		/// <summary>
		/// Gets or sets the value of the Stack Pointer.
		/// </summary>
		public ushort? SP { get; set; }

		/// <summary>
		/// Gets or sets the value of the AF' shadow register pair.
		/// </summary>
		public ushort? AF_Shadow { get; set; }

		/// <summary>
		/// Gets or sets the value of the BC' shadow register pair.
		/// </summary>
		public ushort? BC_Shadow { get; set; }

		/// <summary>
		/// Gets or sets the value of the DE' shadow register pair.
		/// </summary>
		public ushort? DE_Shadow { get; set; }

		/// <summary>
		/// Gets or sets the value of the HL' shadow register pair.
		/// </summary>
		public ushort? HL_Shadow { get; set; }

		/// <summary>
		/// Gets or sets the value of the Interrupt Vector register.
		/// </summary>
		public byte? I { get; set; }

		/// <summary>
		/// Gets or sets the value of the Memory Refresh register.
		/// </summary>
		public byte? R { get; set; }

		/// <summary>
		/// Gets or sets the value of the Interrupt Flip-flop 1.
		/// </summary>
		internal bool? IFF1 { get; set; }

		/// <summary>
		/// Gets or sets the value of the Interrupt Flip-flop 2.
		/// </summary>
		internal bool? IFF2 { get; set; }

		/// <summary>
		/// Gets or sets the value of the Interrupt Mode.
		/// </summary>
		internal InterruptMode? InterruptMode { get; set; }


		/// <summary>
		/// Transfer values from this <see cref="CPUState"/> instance into the settings in
		/// specified <see cref="CPU"/> instance.
		/// </summary>
		/// <remarks>
		/// Only non-null values in this <see cref="CPUState"/> instance are transferred to the <see cref="CPU"/>.
		/// </remarks>
		/// <param name="cpu">The <see cref="CPU"/> instance into which the state values should be transferred.</param>
		public void TransferStateToCPU(CPU cpu)
		{
			if (AF.HasValue)
			{
				cpu.AF = AF.Value;
			}
			if (BC.HasValue)
			{
				cpu.BC = BC.Value;
			}
			if (DE.HasValue)
			{
				cpu.DE = DE.Value;
			}
			if (HL.HasValue)
			{
				cpu.HL = HL.Value;
			}
			if (IX.HasValue)
			{
				cpu.IX = IX.Value;
			}
			if (IY.HasValue)
			{
				cpu.IY = IY.Value;
			}
			if (PC.HasValue)
			{
				cpu.PC = PC.Value;
			}
			if (SP.HasValue)
			{
				cpu.SP = SP.Value;
			}
			if (AF_Shadow.HasValue)
			{
				cpu.AF_Shadow = AF_Shadow.Value;
			}
			if (BC_Shadow.HasValue)
			{
				cpu.BC_Shadow = BC_Shadow.Value;
			}
			if (DE_Shadow.HasValue)
			{
				cpu.DE_Shadow = DE_Shadow.Value;
			}
			if (HL_Shadow.HasValue)
			{
				cpu.HL_Shadow = HL_Shadow.Value;
			}
			if (I.HasValue)
			{
				cpu.I = I.Value;
			}
			if (R.HasValue)
			{
				cpu.R = R.Value;
			}
			if (IFF1.HasValue)
			{
				cpu.IFF1 = IFF1.Value;
			}
			if (IFF2.HasValue)
			{
				cpu.IFF2 = IFF2.Value;
			}
			if (InterruptMode.HasValue)
			{
				cpu.InterruptMode = InterruptMode.Value;
			}
		}

		/// <summary>
		/// Transfer values from the specified <see cref="CPU"/> instance into this <see cref="CPUState"/> instance.
		/// </summary>
		/// <param name="cpu">The <see cref="CPU"/> instance from which the state values should be transferred.</param>
		public void TransferStateFromCPU(CPU cpu)
		{
			AF = cpu.AF;
			BC = cpu.BC;
			DE = cpu.DE;
			HL = cpu.HL;
			IX = cpu.IX;
			IY = cpu.IY;
			PC = cpu.PC;
			SP = cpu.SP;
			AF_Shadow = cpu.AF_Shadow;
			BC_Shadow = cpu.BC_Shadow;
			DE_Shadow = cpu.DE_Shadow;
			HL_Shadow = cpu.HL_Shadow;
			I = cpu.I;
			R = cpu.R;
			IFF1 = cpu.IFF1;
			IFF2 = cpu.IFF2;
			InterruptMode = cpu.InterruptMode;
		}

	}
}

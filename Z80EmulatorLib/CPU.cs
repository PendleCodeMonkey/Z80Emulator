using PendleCodeMonkey.Z80EmulatorLib.Enumerations;

namespace PendleCodeMonkey.Z80EmulatorLib
{
	/// <summary>
	/// Implementation of the <see cref="CPU"/> class.
	/// </summary>
	public class CPU
	{
		#region Constructor

		/// <summary>
		/// Initializes a new instance of the <see cref="CPU"/> class.
		/// </summary>
		public CPU()
		{
			Reset();
		}

		#endregion

		#region Properties

		// *************************
		// General purpose registers
		// *************************

		/// <summary>
		/// Gets or sets the value of the A register.
		/// </summary>
		internal byte A { get; set; }

		/// <summary>
		/// Gets or sets the value of the F register.
		/// </summary>
		internal ProcessorFlags F { get; set; }

		/// <summary>
		/// Gets or sets the value of the B register.
		/// </summary>
		internal byte B { get; set; }

		/// <summary>
		/// Gets or sets the value of the C register.
		/// </summary>
		internal byte C { get; set; }

		/// <summary>
		/// Gets or sets the value of the D register.
		/// </summary>
		internal byte D { get; set; }

		/// <summary>
		/// Gets or sets the value of the E register.
		/// </summary>
		internal byte E { get; set; }

		/// <summary>
		/// Gets or sets the value of the H register.
		/// </summary>
		internal byte H { get; set; }

		/// <summary>
		/// Gets or sets the value of the L register.
		/// </summary>
		internal byte L { get; set; }


		// *************************
		// Special purpose registers
		// *************************

		/// <summary>
		/// Gets or sets the value of the IX register.
		/// </summary>
		internal ushort IX { get; set; }

		/// <summary>
		/// Gets or sets the value of the IY register.
		/// </summary>
		internal ushort IY { get; set; }

		/// <summary>
		/// Gets or sets the value of the Program Counter.
		/// </summary>
		internal ushort PC { get; set; }

		/// <summary>
		/// Gets or sets the value of the Stack Pointer.
		/// </summary>
		internal ushort SP { get; set; }

		/// <summary>
		/// Gets or sets the value of the Interrupt Vector register.
		/// </summary>
		internal byte I { get; set; }

		/// <summary>
		/// Gets or sets the value of the Memory Refresh register.
		/// </summary>
		internal byte R { get; set; }

		/// <summary>
		/// Gets or sets the value of the Interrupt Flip-flop 1.
		/// </summary>
		internal bool IFF1 { get; set; }

		/// <summary>
		/// Gets or sets the value of the Interrupt Flip-flop 2.
		/// </summary>
		internal bool IFF2 { get; set; }

		/// <summary>
		/// Gets or sets the value of the Interrupt Mode.
		/// </summary>
		internal InterruptMode InterruptMode { get; set; }

		/// <summary>
		/// Gets or sets a value indicating if the CPU is currently halted.
		/// </summary>
		internal bool IsHalted { get; set; }


		// *********************
		// 16-bit register pairs
		// *********************

		/// <summary>
		/// Gets or sets the value of the AF register pair.
		/// </summary>
		internal ushort AF
		{
			get => (ushort)(A << 8 | (byte)F);
			set
			{
				A = (byte)(value >> 8);
				F = (ProcessorFlags)(value & 0xFF);
			}
		}

		/// <summary>
		/// Gets or sets the value of the BC register pair.
		/// </summary>
		internal ushort BC
		{
			get => (ushort)(B << 8 | C);
			set
			{
				B = (byte)(value >> 8);
				C = (byte)value;
			}
		}

		/// <summary>
		/// Gets or sets the value of the DE register pair.
		/// </summary>
		internal ushort DE
		{
			get => (ushort)(D << 8 | E);
			set
			{
				D = (byte)(value >> 8);
				E = (byte)value;
			}
		}

		/// <summary>
		/// Gets or sets the value of the HL register pair.
		/// </summary>
		internal ushort HL
		{
			get => (ushort)(H << 8 | L);
			set
			{
				H = (byte)(value >> 8);
				L = (byte)value;
			}
		}


		// **********************
		// Shadow register set
		// **********************

		/// <summary>
		/// Gets or sets the value of the AF' register.
		/// </summary>
		internal ushort AF_Shadow { get; set; }

		/// <summary>
		/// Gets or sets the value of the BC' register.
		/// </summary>
		internal ushort BC_Shadow { get; set; }

		/// <summary>
		/// Gets or sets the value of the DE' register.
		/// </summary>
		internal ushort DE_Shadow { get; set; }

		/// <summary>
		/// Gets or sets the value of the HL' register.
		/// </summary>
		internal ushort HL_Shadow { get; set; }


		// **********************
		// Flag helper properties
		// **********************

		/// <summary>
		/// Gets or sets the value of the Carry flag.
		/// </summary>
		internal bool CarryFlag
		{
			get => F.HasFlag(ProcessorFlags.Carry);
			set
			{
				if (value)
				{
					F |= ProcessorFlags.Carry;
				}
				else
				{
					F &= ~ProcessorFlags.Carry;
				}
			}
		}

		/// <summary>
		/// Gets or sets the value of the Subtract flag.
		/// </summary>
		internal bool SubtractFlag
		{
			get => F.HasFlag(ProcessorFlags.Subtract);
			set
			{
				if (value)
				{
					F |= ProcessorFlags.Subtract;
				}
				else
				{
					F &= ~ProcessorFlags.Subtract;
				}
			}
		}

		/// <summary>
		/// Gets or sets the value of the Parity/Overflow flag.
		/// </summary>
		internal bool ParityOverflowFlag
		{
			get => F.HasFlag(ProcessorFlags.ParityOverflow);
			set
			{
				if (value)
				{
					F |= ProcessorFlags.ParityOverflow;
				}
				else
				{
					F &= ~ProcessorFlags.ParityOverflow;
				}
			}
		}

		/// <summary>
		/// Gets or sets the value of the Half Carry flag.
		/// </summary>
		internal bool HalfCarryFlag
		{
			get => F.HasFlag(ProcessorFlags.HalfCarry);
			set
			{
				if (value)
				{
					F |= ProcessorFlags.HalfCarry;
				}
				else
				{
					F &= ~ProcessorFlags.HalfCarry;
				}
			}
		}

		/// <summary>
		/// Gets or sets the value of the Zero flag.
		/// </summary>
		internal bool ZeroFlag
		{
			get => F.HasFlag(ProcessorFlags.Zero);
			set
			{
				if (value)
				{
					F |= ProcessorFlags.Zero;
				}
				else
				{
					F &= ~ProcessorFlags.Zero;
				}
			}
		}

		/// <summary>
		/// Gets or sets the value of the Sign flag.
		/// </summary>
		internal bool SignFlag
		{
			get => F.HasFlag(ProcessorFlags.Sign);
			set
			{
				if (value)
				{
					F |= ProcessorFlags.Sign;
				}
				else
				{
					F &= ~ProcessorFlags.Sign;
				}
			}
		}

		#endregion

		#region methods

		/// <summary>
		/// Increment the value of the Program Counter.
		/// </summary>
		public void IncrementPC()
		{
			PC++;
		}

		/// <summary>
		///	Add the specified offset value to the Program Counter (offset can be negative).
		/// </summary>
		/// <param name="offset">The offset value to be added to the Program Counter (in the range -128 to 127).</param>
		public void AddOffsetToPC(sbyte offset)
		{
			PC = (ushort)(PC + offset);
		}

		/// <summary>
		/// Resets the CPU to its default settings.
		/// </summary>
		internal void Reset()
		{
			AF = 0;
			BC = 0;
			DE = 0;
			HL = 0;
			IX = 0;
			IY = 0;
			I = R = 0;
			IFF1 = IFF2 = false;
			InterruptMode = InterruptMode.Mode0;
			PC = 0;
			SP = 0;
			AF_Shadow = 0;
			BC_Shadow = 0;
			DE_Shadow = 0;
			HL_Shadow = 0;
		}

		/// <summary>
		/// Exchange the standard and shadow register set values (for the EXX instruction)
		/// </summary>
		internal void ExchangeRegPairsWithShadow()
		{
			ushort temp = BC;
			BC = BC_Shadow;
			BC_Shadow = temp;

			temp = DE;
			DE = DE_Shadow;
			DE_Shadow = temp;

			temp = HL;
			HL = HL_Shadow;
			HL_Shadow = temp;
		}

		/// <summary>
		/// Exchange the AF register with AF' (for the EX AF,AF' instruction)
		/// </summary>
		internal void ExchangeAFWithShadowAF()
		{
			ushort temp = AF;
			AF = AF_Shadow;
			AF_Shadow = temp;
		}

		/// <summary>
		/// Read the value of the register corresponding to the specified numeric index value.
		/// </summary>
		/// <remarks>
		/// The mapping between numeric values and registers is as follows:
		///   0 - B
		///   1 - C
		///   2 - D
		///   3 - E
		///   4 - H
		///   5 - L
		///   7 - A
		/// </remarks>
		/// <param name="r">The numeric value corresponding to the register whose value is to be returned.</param>
		/// <returns>The value of the requested register (or zero if an invalid index value is supplied).</returns>
		internal byte ReadRegister(byte r)
		{
			return r switch
			{
				0x00 => B,
				0x01 => C,
				0x02 => D,
				0x03 => E,
				0x04 => H,
				0x05 => L,
				0x07 => A,
				_ => 0,
			};
		}

		/// <summary>
		/// Write a value to the register corresponding to the specified numeric index value.
		/// </summary>
		/// <param name="r">The numeric value corresponding to the register whose value is to be set.</param>
		/// <param name="value">The value to be used for the specified register.</param>
		internal void WriteRegister(byte r, byte value)
		{
			switch (r)
			{
				case 0x00:
					B = value;
					break;
				case 0x01:
					C = value;
					break;
				case 0x02:
					D = value;
					break;
				case 0x03:
					E = value;
					break;
				case 0x04:
					H = value;
					break;
				case 0x05:
					L = value;
					break;
				case 0x07:
					A = value;
					break;
				default:
					break;
			}
		}

		/// <summary>
		/// Read the value of the register pair that corresponds to the specified numeric index value.
		/// </summary>
		/// <remarks>
		/// The mapping between numeric values and register pairs is as follows:
		///   0 - BC
		///   1 - DE
		///   2 - HL
		///   3 - SP or AF  (Note: some instructions work with AF instead of SP)
		/// </remarks>
		/// <param name="r">The numeric value corresponding to the register pair whose value is to be returned.</param>
		/// <param name="AFInsteadOfSP">If <c>true</c>, the value of the AF register is returned for index value 3, otherwise SP is returned.</param>
		/// <returns>The value of the requested register pair (or zero if an invalid index value is supplied).</returns>
		internal ushort ReadRegisterPair(byte r, bool AFInsteadOfSP = false)
		{
			return r switch
			{
				0x00 => BC,
				0x01 => DE,
				0x02 => HL,
				0x03 => AFInsteadOfSP ? AF : SP,
				_ => 0,
			};
		}

		/// <summary>
		/// Write a value to the register pair that corresponds to the specified numeric index value.
		/// </summary>
		/// <param name="r">The numeric value corresponding to the register pair whose value is to be set.</param>
		/// <param name="value">The value to which the specified register pair is to be set.</param>
		/// <param name="AFInsteadOfSP">If <c>true</c>, the value of the AF register is set for index value 3, otherwise SP is set.</param>
		internal void WriteRegisterPair(byte r, ushort value, bool AFInsteadOfSP = false)
		{
			switch (r)
			{
				case 0x00:
					BC = value;
					break;
				case 0x01:
					DE = value;
					break;
				case 0x02:
					HL = value;
					break;
				case 0x03:
					if (AFInsteadOfSP)
					{
						AF = value;
					}
					else
					{
						SP = value;
					}
					break;
				default:
					break;
			}
		}

		/// <summary>
		/// Evaluate the result of the condition corresponding to the specified numeric index value.
		/// </summary>
		/// <remarks>
		/// The mapping between numeric values and conditions is as follows:
		///   0 - Not Zero
		///   1 - Zero
		///   2 - Not Carry
		///   3 - Carry
		///   4 - Odd Parity / No Overflow
		///   5 - Even Parity / Overflow
		///   6 - Positive
		///   7 - Negative
		/// </remarks>
		/// <param name="condition">The numeric index value corresponding to the condition to be evaluated.</param>
		/// <returns>The result of the evaluation of the specified condition.</returns>
		internal bool EvaluateCondition(byte condition)
		{
			return condition switch
			{
				0 => !ZeroFlag,
				1 => ZeroFlag,
				2 => !CarryFlag,
				3 => CarryFlag,
				4 => !ParityOverflowFlag,
				5 => ParityOverflowFlag,
				6 => !SignFlag,
				7 => SignFlag,
				_ => false,
			};
			;
		}

		/// <summary>
		/// Gets the Zero Page address corresponding to the specified numeric index value.
		/// NOTE: This is only used for the RST instruction.
		/// </summary>
		/// <param name="value">The numeric index value corresponding to the Zero Page address to be returned.</param>
		/// <returns>The Zero Page address corresponding to the specified numeric index value.</returns>
		internal ushort GetPageZeroAddress(byte value)
		{
			return value switch
			{
				0 => 0x0000,
				1 => 0x0008,
				2 => 0x0010,
				3 => 0x0018,
				4 => 0x0020,
				5 => 0x0028,
				6 => 0x0030,
				7 => 0x0038,
				_ => 0x0000,
			};
		}

		#endregion
	}
}

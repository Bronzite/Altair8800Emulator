using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

namespace Altair8800Emulator
{
    /// <summary>
    /// Main class for emulating an Altair 8800 computer.
    /// </summary>
    public class Machine
    {
        public Machine()
        {
            //Allocate 64KB Main Memory
            MainMemory = new byte[65536];
            vTable = new Action[256];
            DeviceBus = new Device[256];
            Interrupts = true;

            LoadvTable();
        }

        //64K block of main memory
        public byte[] MainMemory { get; set; }

        //Load memory state
        public void LoadCode(byte[] bData)
        {
            Array.Copy(bData, 0, MainMemory, 0, bData.Length);
        }

        //Define accumulators
        public byte Accumulator;
        public byte RegisterB;
        public byte RegisterC;
        public byte RegisterD;
        public byte RegisterE;
        public byte RegisterH;
        public byte RegisterL;
        public bool CarryBit;
        public bool AuxBit;
        public bool SignBit;
        public bool ZeroBit;
        public bool ParityBit;
        public UInt16 ProgramCounter;
        public UInt16 StackPointer;
        public bool Interrupts; 
        public Device[] DeviceBus;

        /// <summary>
        /// Runs the next instruction at the Program Counter.
        /// </summary>
        public void Step()
        {
            byte InstructionRegister = MainMemory[ProgramCounter];

            vTable[InstructionRegister].Invoke();
            ProgramCounter++;

        }

        /// <summary>
        /// Add two 16-bit number together.
        /// </summary>
        /// <param name="h1">First High Byte</param>
        /// <param name="l1">First Low Byte</param>
        /// <param name="h2">Second High Byte</param>
        /// <param name="l2">Second Low Byte</param>
        /// <returns>Array of two bytes containing the result</returns>
        private byte[] DoubleAdd(byte h1,byte l1, byte h2, byte l2)
        {
            UInt32 operand1 = (UInt32)(h1 << 8) + l1;
            UInt32 operand2 = (UInt32)(h2 << 8) + l2;
            UInt32 result = operand1+ operand2;
            if(result > 0xFFFF) CarryBit= true;
            result = (UInt16)(result & 0xFFFF);
            byte[] retval = new byte[2];
            retval[0] =(byte)(result >> 8);
            retval[1] = (byte)(result & 0xFF);
            return retval;
        }

        /// <summary>
        /// The table of instructions.
        /// </summary>
        private Action[] vTable = null;

        /// <summary>
        /// Read the next two bytes from the progam counter and return the
        /// location as a UInt16
        /// </summary>
        /// <returns>Location represented by read bytes.</returns>
        public UInt16 ReadMemoryLocationFromProgramCounter()
        {
            byte bLow = MainMemory[++ProgramCounter];
            byte bHigh = MainMemory[++ProgramCounter];
            return GetMemoryLocation(bHigh, bLow);
        }

        /// <summary>
        /// Get a 16-bit memory location from two bytes.
        /// </summary>
        /// <param name="bHigh">High byte</param>
        /// <param name="bLow">Low byte</param>
        /// <returns>Memory location represented by the bytes given.</returns>
        public UInt16 GetMemoryLocation(byte bHigh, byte bLow)
        {
            UInt16 iLocation = (UInt16)((bHigh << 8) + bLow);
            return iLocation;
        }


        /// <summary>
        /// Copy src value into Destination reference.
        /// </summary>
        /// <param name="dst">Byte to update</param>
        /// <param name="src">Value to copy to dst</param>
        public void MOV(ref byte dst, byte src) { dst = src; }
        /// <summary>
        /// Get the Memory Location from H/L and store the src byte there.
        /// </summary>
        /// <param name="src">The value to store.</param>
        public void MOVToMem(byte src) { int iAddress = GetMemoryLocation(RegisterH, RegisterL); MainMemory[iAddress] = src; }
        /// <summary>
        /// Get the value from the memory location at H/L and store to the
        /// reference register.
        /// </summary>
        /// <param name="dst">The register to store the value in.</param>
        public void MOVFromMem(ref byte dst) { int iAddress = GetMemoryLocation(RegisterH, RegisterL); dst = MainMemory[iAddress]; }
        /// <summary>
        /// Adds the given byte to the Accumulator.
        /// </summary>
        /// <param name="amount"></param>
        public void ADD(byte amount) { int Acc = (int)Accumulator + (int)amount;
            if (Acc > 255) CarryBit = true;
            Accumulator = (byte)(Acc & 0xFF);
        }

        public void ADC(byte amount)
        {
            int Acc = (int)Accumulator + (int)amount + (CarryBit?1:0);
            if (Acc > 255) CarryBit = true;
            Accumulator = (byte)(Acc & 0xFF);
        }

        public void SUB(byte amount)
        {
            ADD(TwosComplement(amount));
        }

        public void SBB(byte amount)
        {
            ADD(TwosComplement((byte)(amount + (CarryBit ? 1 : 0))));
        }
        private byte TwosComplement(byte b)
        {
            return (byte)(b ^ 0xFF + 1);
        }

        public void ANA(byte amount) { Accumulator = (byte)(Accumulator & amount); }
        public void ORA(byte amount) { Accumulator = (byte)(Accumulator | amount); }
        public void XRA(byte amount) { Accumulator = (byte)(Accumulator ^ amount); }
        public void CMP(byte amount) { int CompareValue = (Accumulator + TwosComplement(amount));
            if (CompareValue == 0)
            {
                ZeroBit = true;
            }
            if (CompareValue > 0) CarryBit = false;
            if (CompareValue < 0) CarryBit = true;
        }

        public void PUSH(byte bHigh, byte bLow)
        {
            MainMemory[StackPointer++] = bHigh;
            MainMemory[StackPointer++] = bLow;
        }

        public void POP(ref byte bHigh,ref byte bLow)
        {
            bLow = MainMemory[--StackPointer];
            bHigh = MainMemory[--StackPointer];
        }

        public void JMP()
        {
            byte bLow = MainMemory[++ProgramCounter];
            byte bHigh = MainMemory[++ProgramCounter]; 
            ProgramCounter = (UInt16)(GetMemoryLocation(bHigh, bLow)-1);
        }

        public void CALL()
        {
            byte bLow = MainMemory[++ProgramCounter];
            byte bHigh = MainMemory[++ProgramCounter];

            CALL(bHigh, bLow);
        }
        public void CALL(byte bHigh, byte bLow) {
            byte bPCHigh = (byte)(ProgramCounter >> 8);
            byte bPCLow  = (byte)(ProgramCounter & 0xFF);
            PUSH(bPCHigh, bPCLow);
            ProgramCounter = (UInt16)((bHigh << 8) + bLow);
        }
        public void RETURN() {
            byte bPCHigh=0, bPCLow=0;
            POP(ref bPCHigh, ref bPCLow);
            ProgramCounter = (UInt16)((bPCHigh << 8) + bPCLow);
        }

        public void RST(int iBlock)
        {
            byte bHigh = (byte)(ProgramCounter >> 8);
            byte bLow = (byte)(ProgramCounter & 0xFF);

            PUSH(bHigh, bLow);
            ProgramCounter = (UInt16)(8 * iBlock);
        }
        public byte GetMemoryValue() { return MainMemory[GetMemoryLocation(RegisterH, RegisterL)]; }

        public byte StatusByte
        {
            get
            {
                byte retval = 0b00000010;

                if (CarryBit) retval =  (byte)(retval | 0b00000001);
                if (ParityBit) retval = (byte)(retval | 0b00000100);
                if (AuxBit) retval = (byte)(retval | 0b00010000);
                if (ZeroBit) retval = (byte)(retval | 0b01000000);
                if (SignBit) retval = (byte)(retval | 0b10000000);

                return retval;
            }

            set
            {
                CarryBit = (value & 0b00000001) == 0b00000001;
                ParityBit = (value & 0b00000100) == 0b00000100;
                AuxBit = (value & 0b00010000) == 0b00010000;
                ZeroBit = (value & 0b01000000) == 0b01000000;
                SignBit = (value & 0b10000000) == 0b10000000;
            }
        }

        public void LoadvTable()
        {

            vTable[0x00] = () => { };
            
            //LXI Calls
            vTable[0x01] =
                () => {
                    //LXI BC
                    RegisterC = MainMemory[++ProgramCounter];
                    RegisterB = MainMemory[++ProgramCounter];
                };
            vTable[0x11] =
                () => {
                    //LXI DE
                    RegisterE = MainMemory[++ProgramCounter];
                    RegisterD = MainMemory[++ProgramCounter];
                };
            vTable[0x21] =
                () => {
                    //LXI HL
                    RegisterL = MainMemory[++ProgramCounter];
                    RegisterH = MainMemory[++ProgramCounter];
                };
            vTable[0x31] =
                () => {
                    //LXI Stack Pointer
                    byte bLow = MainMemory[++ProgramCounter];
                    byte bHigh = MainMemory[++ProgramCounter];
                    
                    StackPointer = (ushort)((bHigh << 8 )+ bLow);
                    
                };

            vTable[0x02] = () => //STAX B
                {
                    MainMemory[GetMemoryLocation(RegisterB, RegisterC)] = Accumulator;
                };
            vTable[0x12] = () => //STAX D
            {
                MainMemory[GetMemoryLocation(RegisterD, RegisterE)] = Accumulator;
            };

            vTable[0x22] = () => //SHLD a16
            {
                int iAddress = ReadMemoryLocationFromProgramCounter();
                MainMemory[iAddress] = RegisterH;
                MainMemory[iAddress + 1] = RegisterL;
            };
            vTable[0x32] = () => //STA a16
            {
                int iAddress = ReadMemoryLocationFromProgramCounter();
                MainMemory[iAddress] = Accumulator;
            };

            vTable[0x03] = () => //INX B
            {
                if (RegisterC == 255) RegisterB++;
                RegisterC++;
            };
            vTable[0x13] = () => //INX D
            {
                    if (RegisterE == 255) RegisterD++;
                    RegisterE++;
               
            };
            vTable[0x23] = () => // INX H
            {
                if (RegisterL == 255) RegisterH++;
                RegisterL++;
            };
            vTable[0x33] = () => //IXN SP
            {
                StackPointer++;
            };

            vTable[0x04] = () => //INR B
            {
                if (RegisterB == 255) CarryBit = true; else CarryBit = false;
                RegisterB++;
            };
            vTable[0x14] = () => //INR D
            {
                if (RegisterD == 255) CarryBit = true; else CarryBit = false;
                RegisterD++;

            };
            vTable[0x24] = () => // INR H
            {
                if (RegisterH == 255) CarryBit = true; else CarryBit = false;
                RegisterH++;
            };
            vTable[0x34] = () => //INR M
            {
                int iAddress = GetMemoryLocation(RegisterH, RegisterL);
                if (MainMemory[iAddress]==255) CarryBit = true; else CarryBit = false;
                MainMemory[iAddress]++;
            };

            vTable[0x05] = () => //DCR B
            {
                if (RegisterB == 0) CarryBit = true; else CarryBit = false;
                RegisterB--;
            };
            vTable[0x15] = () => //DCR D
            {
                if (RegisterD == 0) CarryBit = true; else CarryBit = false;
                RegisterD--;

            };
            vTable[0x25] = () => // DCR H
            {
                if (RegisterH == 0) CarryBit = true; else CarryBit = false;
                RegisterH--;
            };
            vTable[0x35] = () => //DCR M
            {
                int iAddress = GetMemoryLocation(RegisterH, RegisterL);
                if (MainMemory[iAddress] == 0) CarryBit = true; else CarryBit = false;
                MainMemory[iAddress]--;
            };

            vTable[0x06] = () => //MVI B
            {
                RegisterB = MainMemory[++ProgramCounter];
            };
            vTable[0x16] = () => //MVI D
            {
                RegisterD = MainMemory[++ProgramCounter];

            };
            vTable[0x26] = () => //MVI H
            {
                RegisterH = MainMemory[++ProgramCounter];
            };
            vTable[0x36] = () => //MVI M
            {
                int iAddress = GetMemoryLocation(RegisterH, RegisterL);
                MainMemory[iAddress] = MainMemory[++ProgramCounter];
            };

            vTable[0x07] = () => //RLC
            {
                CarryBit = (Accumulator & 0b10000000) == 0b10000000;
                Accumulator = (byte)(Accumulator << 1);
                if (CarryBit) Accumulator++;
            };
            vTable[0x17] = () => //RAL
            {
                bool oldCarry = CarryBit;
                CarryBit = (Accumulator & 0b10000000) == 0b10000000;
                Accumulator = (byte)(Accumulator << 1);
                if (oldCarry) Accumulator++;
            };

            vTable[0x27] = () => //DAA
            {
                byte lowByte = (byte)(Accumulator & 0b00001111);
                byte highByte = (byte)(Accumulator >> 4);
                if(lowByte > 9 || AuxBit)
                {
                    CarryBit = true;
                    lowByte += 6;
                }
                if(highByte>9 || CarryBit)
                {
                    highByte += 6;
                    CarryBit = true;
                }
            };

            vTable[0x37] = () => //STC
            {
                CarryBit = true;
            };

            vTable[0x08] = () => //NOP
            {
               
            };
            vTable[0x18] = () => //NOP
            {
                
            };

            vTable[0x28] = () => //NOP
            {

            };

            vTable[0x38] = () => //NOP
            {
 
            };

            vTable[0x09] = () => //DAD B
            {
                byte[] result = DoubleAdd(RegisterB, RegisterC, RegisterH, RegisterL);
                RegisterH = result[0];
                RegisterL = result[1];
            };
            vTable[0x19] = () => //DAD D
            {
                byte[] result = DoubleAdd(RegisterD, RegisterE, RegisterH, RegisterL);
                RegisterH = result[0];
                RegisterL = result[1];
            };

            vTable[0x29] = () => //DAD H
            {
                byte[] result = DoubleAdd(RegisterH, RegisterL, RegisterH, RegisterL);
                RegisterH = result[0];
                RegisterL = result[1];
            };

            vTable[0x39] = () => //DAD Flags & A
            {
                byte[] result = DoubleAdd(StatusByte, Accumulator, RegisterH, RegisterL);
                RegisterH = result[0];
                RegisterL = result[1];
            };

            vTable[0x0A] = () => //LDAX B
            {
                int iAddress = GetMemoryLocation(RegisterB, RegisterC);
                Accumulator = MainMemory[iAddress];
            };
            vTable[0x1A] = () => //LDAX D
            {
                int iAddress = GetMemoryLocation(RegisterD, RegisterE);
                Accumulator = MainMemory[iAddress];
            };

            vTable[0x2A] = () => //LHLD a16
            {
                int iAddress = ReadMemoryLocationFromProgramCounter();
                RegisterL = MainMemory[iAddress];
                RegisterH = MainMemory[iAddress+1];
            };

            vTable[0x3A] = () => //LDA a16
            {
                int iAddress = ReadMemoryLocationFromProgramCounter();
                Accumulator = MainMemory[iAddress];
            };

            vTable[0x0B] = () => //DCX B
            {
                if (RegisterC == 0) RegisterB--;
                RegisterC--;
            };
            vTable[0x1B] = () => //DCX D
            {
                if (RegisterE == 0) RegisterD--;
                RegisterE--;
            };

            vTable[0x2B] = () => //DCX H
            {
                if (RegisterL == 0) RegisterL--;
                RegisterL--;
            };

            vTable[0x3B] = () => //DCX FLAG & A
            {
                Accumulator--;
            };

            vTable[0x0C] = () => //INR C
            {
                if (RegisterC == 255) CarryBit = true; else CarryBit = false;
                RegisterC++;
            };
            vTable[0x1C] = () => //INR E
            {
                if (RegisterE == 255) CarryBit = true; else CarryBit = false;
                RegisterE++;

            };
            vTable[0x2C] = () => // INR L
            {
                if (RegisterL == 255) CarryBit = true; else CarryBit = false;
                RegisterL++;
            };
            vTable[0x3C] = () => //INR A
            {
                if (Accumulator == 255) CarryBit = true; else CarryBit = false;
                Accumulator++;
           };

            vTable[0x0D] = () => //DCR C
            {
                if (RegisterC == 0) CarryBit = true; else CarryBit = false;
                RegisterC--;
            };
            vTable[0x1D] = () => //DCR E
            {
                if (RegisterE == 0) CarryBit = true; else CarryBit = false;
                RegisterE--;

            };
            vTable[0x2D] = () => // DCR L
            {
                if (RegisterL == 0) CarryBit = true; else CarryBit = false;
                RegisterL --;
            };
            vTable[0x3D] = () => //DCR A
            {
                if (Accumulator == 0) CarryBit = true; else CarryBit = false;
                Accumulator--;
            };
            vTable[0x0E] = () => //MVI C
            {
                RegisterC = MainMemory[++ProgramCounter];

            };
            vTable[0x1E] = () => //MVI E
            {
                RegisterE = MainMemory[++ProgramCounter];

            };
            vTable[0x2E] = () => //MVI L
            {
                RegisterL = MainMemory[++ProgramCounter];
            };
            vTable[0x3E] = () => //MVI A
            {
                Accumulator = MainMemory[++ProgramCounter];
            };
            vTable[0x0F] = () => //RRC
            {
                CarryBit = (Accumulator & 0b00000001) == 0b00000001;
                Accumulator = (byte)(Accumulator >> 1);
                if (CarryBit) Accumulator += 0b10000000;
            };
            vTable[0x1F] = () => //RAR
            {
                bool oldCarry = CarryBit;
                CarryBit = (Accumulator & 0b00000001) == 0b00000001;
                Accumulator = (byte)(Accumulator >> 1);
                if (oldCarry) Accumulator += 0b10000000;
            };

            vTable[0x2F] = () => //CMA
            {
                Accumulator = (byte)~Accumulator;
            };

            vTable[0x3F] = () => //CMC
            {
                CarryBit = !CarryBit;
            };

            vTable[0x40] = () => { MOV(ref RegisterB, RegisterB); };
            vTable[0x50] = () => { MOV(ref RegisterD, RegisterB); };
            vTable[0x60] = () => { MOV(ref RegisterH, RegisterB); };
            vTable[0x70] = () => { MOVToMem(RegisterB); };
            
            vTable[0x41] = () => { MOV(ref RegisterB, RegisterC); };
            vTable[0x51] = () => { MOV(ref RegisterD, RegisterC); };
            vTable[0x61] = () => { MOV(ref RegisterH, RegisterC); };
            vTable[0x71] = () => { MOVToMem(RegisterC); };
            
            vTable[0x42] = () => { MOV(ref RegisterB, RegisterD); };
            vTable[0x52] = () => { MOV(ref RegisterD, RegisterD); };
            vTable[0x62] = () => { MOV(ref RegisterH, RegisterD); };
            vTable[0x72] = () => { MOVToMem(RegisterD); };

            vTable[0x43] = () => { MOV(ref RegisterB, RegisterE); };
            vTable[0x53] = () => { MOV(ref RegisterD, RegisterE); };
            vTable[0x63] = () => { MOV(ref RegisterH, RegisterE); };
            vTable[0x73] = () => { MOVToMem(RegisterE); };

            vTable[0x44] = () => { MOV(ref RegisterB, RegisterH); };
            vTable[0x54] = () => { MOV(ref RegisterD, RegisterH); };
            vTable[0x64] = () => { MOV(ref RegisterH, RegisterH); };
            vTable[0x74] = () => { MOVToMem(RegisterH); };

            vTable[0x45] = () => { MOV(ref RegisterB, RegisterL); };
            vTable[0x55] = () => { MOV(ref RegisterD, RegisterL); };
            vTable[0x65] = () => { MOV(ref RegisterH, RegisterL); };
            vTable[0x75] = () => { MOVToMem(RegisterL); };

            vTable[0x46] = () => { MOVFromMem(ref RegisterB); };
            vTable[0x56] = () => { MOVFromMem(ref RegisterD); };
            vTable[0x66] = () => { MOVFromMem(ref RegisterH); };
            vTable[0x76] = () => { //HALT
                --ProgramCounter;
                                   };
            vTable[0x47] = () => { MOV(ref RegisterB, Accumulator); };
            vTable[0x57] = () => { MOV(ref RegisterD, Accumulator); };
            vTable[0x67] = () => { MOV(ref RegisterH, Accumulator); };
            vTable[0x77] = () => { MOVToMem(Accumulator); };

            vTable[0x48] = () => { MOV(ref RegisterC, RegisterB); };
            vTable[0x58] = () => { MOV(ref RegisterE, RegisterB); };
            vTable[0x68] = () => { MOV(ref RegisterL, RegisterB); };
            vTable[0x78] = () => { MOV(ref Accumulator, RegisterB); };

            vTable[0x49] = () => { MOV(ref RegisterC, RegisterC); };
            vTable[0x59] = () => { MOV(ref RegisterE, RegisterC); };
            vTable[0x69] = () => { MOV(ref RegisterL, RegisterC); };
            vTable[0x79] = () => { MOV(ref Accumulator, RegisterC); };
            
            vTable[0x4A] = () => { MOV(ref RegisterC, RegisterD); };
            vTable[0x5A] = () => { MOV(ref RegisterE, RegisterD); };
            vTable[0x6A] = () => { MOV(ref RegisterL, RegisterD); };
            vTable[0x7A] = () => { MOV(ref Accumulator, RegisterD); };

            vTable[0x4B] = () => { MOV(ref RegisterC, RegisterE); };
            vTable[0x5B] = () => { MOV(ref RegisterE, RegisterE); };
            vTable[0x6B] = () => { MOV(ref RegisterL, RegisterE); };
            vTable[0x7B] = () => { MOV(ref Accumulator, RegisterE); };

            vTable[0x4C] = () => { MOV(ref RegisterC, RegisterH); };
            vTable[0x5C] = () => { MOV(ref RegisterE, RegisterH); };
            vTable[0x6C] = () => { MOV(ref RegisterL, RegisterH); };
            vTable[0x7C] = () => { MOV(ref Accumulator, RegisterH); };

            vTable[0x4D] = () => { MOV(ref RegisterC, RegisterL); };
            vTable[0x5D] = () => { MOV(ref RegisterE, RegisterL); };
            vTable[0x6D] = () => { MOV(ref RegisterL, RegisterL); };
            vTable[0x7D] = () => { MOV(ref Accumulator, RegisterL); };

            vTable[0x4E] = () => { MOVFromMem(ref RegisterC); };
            vTable[0x5E] = () => { MOVFromMem(ref RegisterE); };
            vTable[0x6E] = () => { MOVFromMem(ref RegisterL); };
            vTable[0x7E] = () => { MOVFromMem(ref Accumulator); };

            vTable[0x4F] = () => { MOV(ref RegisterC, Accumulator); };
            vTable[0x5F] = () => { MOV(ref RegisterE, Accumulator); };
            vTable[0x6F] = () => { MOV(ref RegisterL, Accumulator); };
            vTable[0x7F] = () => { MOV(ref Accumulator, Accumulator); };

            vTable[0x80] = () => { ADD(RegisterB); };
            vTable[0x90] = () => { SUB(RegisterB); };
            vTable[0xA0] = () => { ANA(RegisterB); };
            vTable[0xB0] = () => { ORA(RegisterB); };

            vTable[0x81] = () => { ADD(RegisterC); };
            vTable[0x91] = () => { SUB(RegisterC); };
            vTable[0xA1] = () => { ANA(RegisterC); };
            vTable[0xB1] = () => { ORA(RegisterC); };

            vTable[0x82] = () => { ADD(RegisterD); };
            vTable[0x92] = () => { SUB(RegisterD); };
            vTable[0xA2] = () => { ANA(RegisterD); };
            vTable[0xB2] = () => { ORA(RegisterD); };

            vTable[0x83] = () => { ADD(RegisterE); };
            vTable[0x93] = () => { SUB(RegisterE); };
            vTable[0xA3] = () => { ANA(RegisterE); };
            vTable[0xB3] = () => { ORA(RegisterE); };

            vTable[0x84] = () => { ADD(RegisterH); };
            vTable[0x94] = () => { SUB(RegisterH); };
            vTable[0xA4] = () => { ANA(RegisterH); };
            vTable[0xB4] = () => { ORA(RegisterH); };

            vTable[0x85] = () => { ADD(RegisterL); };
            vTable[0x95] = () => { SUB(RegisterL); };
            vTable[0xA5] = () => { ANA(RegisterL); };
            vTable[0xB5] = () => { ORA(RegisterL); };

            vTable[0x86] = () => { ADD(GetMemoryValue()); };
            vTable[0x96] = () => { SUB(GetMemoryValue()); };
            vTable[0xA6] = () => { ANA(GetMemoryValue()); };
            vTable[0xB6] = () => { ORA(GetMemoryValue()); };

            vTable[0x87] = () => { ADD(Accumulator); };
            vTable[0x97] = () => { SUB(Accumulator); };
            vTable[0xA7] = () => { ANA(Accumulator); };
            vTable[0xB7] = () => { ORA(Accumulator); };

            vTable[0x88] = () => { ADC(RegisterB); };
            vTable[0x98] = () => { SBB(RegisterB); };
            vTable[0xA8] = () => { XRA(RegisterB); };
            vTable[0xB8] = () => { CMP(RegisterB); };

            vTable[0x89] = () => { ADC(RegisterC); };
            vTable[0x99] = () => { SBB(RegisterC); };
            vTable[0xA9] = () => { XRA(RegisterC); };
            vTable[0xB9] = () => { CMP(RegisterC); };

            vTable[0x8A] = () => { ADC(RegisterD); };
            vTable[0x9A] = () => { SBB(RegisterD); };
            vTable[0xAA] = () => { XRA(RegisterD); };
            vTable[0xBA] = () => { CMP(RegisterD); };

            vTable[0x8B] = () => { ADC(RegisterE); };
            vTable[0x9B] = () => { SBB(RegisterE); };
            vTable[0xAB] = () => { XRA(RegisterE); };
            vTable[0xBB] = () => { CMP(RegisterE); };

            vTable[0x8C] = () => { ADC(RegisterH); };
            vTable[0x9C] = () => { SBB(RegisterH); };
            vTable[0xAC] = () => { XRA(RegisterH); };
            vTable[0xBC] = () => { CMP(RegisterH); };

            vTable[0x8D] = () => { ADC(RegisterL); };
            vTable[0x9D] = () => { SBB(RegisterL); };
            vTable[0xAD] = () => { XRA(RegisterL); };
            vTable[0xBD] = () => { CMP(RegisterL); };

            vTable[0x8E] = () => { ADC(GetMemoryValue()); };
            vTable[0x9E] = () => { SBB(GetMemoryValue()); };
            vTable[0xAE] = () => { XRA(GetMemoryValue()); };
            vTable[0xBE] = () => { CMP(GetMemoryValue()); };

            vTable[0x8F] = () => { ADC(Accumulator); };
            vTable[0x9F] = () => { SBB(Accumulator); };
            vTable[0xAF] = () => { XRA(Accumulator); };
            vTable[0xBF] = () => { CMP(Accumulator); };

            vTable[0xC0] = () => { if (!ZeroBit) RETURN();   }; //RNZ
            vTable[0xD0] = () => { if (!CarryBit) RETURN(); }; //RNC
            vTable[0xE0] = () => { if (!ParityBit) RETURN(); }; //RPO
            vTable[0xF0] = () => { if (!SignBit) RETURN(); }; //RP


            vTable[0xC1] = () => { POP(ref RegisterB, ref RegisterC); }; 
            vTable[0xD1] = () => { POP(ref RegisterD, ref RegisterE); }; 
            vTable[0xE1] = () => { POP(ref RegisterH, ref RegisterL); }; 
            vTable[0xF1] = () => { byte bh = 0, bl = 0;
                POP(ref bh, ref bl);
                Accumulator = bl;
                StatusByte = bh;
            };

            vTable[0xC2] = () => { if (!ZeroBit) JMP(); }; //JNZ
            vTable[0xD2] = () => { if (!CarryBit) JMP(); }; //JNC
            vTable[0xE2] = () => { if (!ParityBit) JMP(); }; //JPO
            vTable[0xF2] = () => { if (!SignBit) JMP(); }; //JP

            vTable[0xC3] = () => { JMP(); }; //JMP
            vTable[0xD3] = () => {
                //OUT
                byte bDevNumber = MainMemory[++ProgramCounter];
                if (DeviceBus[bDevNumber] != null) DeviceBus[bDevNumber].WriteByte(Accumulator);
            };
            vTable[0xE3] = () => {
                //XTHL
                byte Low = MainMemory[StackPointer];
                byte High = MainMemory[StackPointer + 1];

                MainMemory[StackPointer] = RegisterL;
                MainMemory[StackPointer + 1] = RegisterH;
                RegisterL = Low;
                RegisterH = High;
            }; 
            vTable[0xF3] = () => { Interrupts = false; }; //DI

            vTable[0xC4] = () => { if (!ZeroBit) CALL(); }; //CNZ
            vTable[0xD4] = () => { if (!CarryBit) CALL(); }; //CNC
            vTable[0xE4] = () => { if (!ParityBit) CALL(); }; //CPO
            vTable[0xF4] = () => { if (!SignBit) CALL(); }; //CP

            vTable[0xC5] = () => { PUSH(RegisterB, RegisterC); }; //PUSH B
            vTable[0xD5] = () => { PUSH(RegisterD, RegisterE); }; //PUSH D
            vTable[0xE5] = () => { PUSH(RegisterH, RegisterL); }; //PUSH H
            vTable[0xF5] = () => { PUSH(StatusByte, Accumulator); }; //PUSH PSW

            vTable[0xC6] = () => { ADD(MainMemory[++ProgramCounter]); }; //ADI
            vTable[0xD6] = () => { SUB(MainMemory[++ProgramCounter]); }; //SUI
            vTable[0xE6] = () => { ANA(MainMemory[++ProgramCounter]); }; // ANI
            vTable[0xF6] = () => { ORA(MainMemory[++ProgramCounter]); }; // ORI

            vTable[0xC7] = () => { RST(0); }; //RST 0
            vTable[0xD7] = () => { RST(2); }; //RST 2
            vTable[0xE7] = () => { RST(4); }; //RST 4
            vTable[0xF7] = () => { RST(6); }; //RST 6

            vTable[0xC8] = () => { if (ZeroBit) RETURN(); }; //RZ
            vTable[0xD8] = () => { if (CarryBit) RETURN(); }; //RC
            vTable[0xE8] = () => { if (ParityBit) RETURN(); }; //RPE
            vTable[0xF8] = () => { if (SignBit) RETURN(); }; //RM


            vTable[0xC9] = () => { RETURN(); }; //RET
            vTable[0xD9] = () => { RETURN(); }; //RET
            vTable[0xE9] = () => { ProgramCounter = (UInt16)((RegisterH << 8) + RegisterL); }; //PCHL
            vTable[0xF9] = () => { StackPointer = (UInt16)((RegisterH << 8) + RegisterL); }; //SPHL

            vTable[0xCA] = () => { if (ZeroBit) JMP(); }; //JZ
            vTable[0xDA] = () => { if (CarryBit) JMP(); }; //JC
            vTable[0xEA] = () => { if (ParityBit) JMP(); }; //JPE
            vTable[0xFA] = () => { if (SignBit) JMP(); }; //JM

            vTable[0xCB] = () => { JMP(); }; //JMP
            vTable[0xDB] = () => {
                //IN
                byte bDevNumber = MainMemory[++ProgramCounter];
                if (DeviceBus[bDevNumber] != null) Accumulator = DeviceBus[bDevNumber].ReadByte();
            };
            vTable[0xEB] = () => {
                //XCHG
                byte Low = RegisterE;
                byte High = RegisterD;

                RegisterE = RegisterL;
                RegisterD = RegisterH;
                RegisterL = Low;
                RegisterH = High;
            };
            vTable[0xFB] = () => { Interrupts = true; }; //EI

            vTable[0xCC] = () => { if (ZeroBit) CALL(); }; //CZ
            vTable[0xDC] = () => { if (CarryBit) CALL(); }; //CC
            vTable[0xEC] = () => { if (ParityBit) CALL(); }; //CPE
            vTable[0xFC] = () => { if (SignBit) CALL(); }; //CM

            vTable[0xCD] = () => { CALL(); }; //CALL
            vTable[0xDD] = () => { CALL(); }; //CALL
            vTable[0xED] = () => { CALL(); }; //CALL
            vTable[0xFD] = () => { CALL(); }; //CALL

            vTable[0xCE] = () => { ADC(MainMemory[++ProgramCounter]); }; //ACI
            vTable[0xDE] = () => { SBB(MainMemory[++ProgramCounter]); }; //SBI
            vTable[0xEE] = () => { XRA(MainMemory[++ProgramCounter]); }; // XRI
            vTable[0xFE] = () => { CMP(MainMemory[++ProgramCounter]); }; // CPI

            vTable[0xCF] = () => { RST(1); }; //RST 1
            vTable[0xDF] = () => { RST(3); }; //RST 3
            vTable[0xEF] = () => { RST(5); }; //RST 5
            vTable[0xFF] = () => { RST(7); }; //RST 7

        }


    }
}
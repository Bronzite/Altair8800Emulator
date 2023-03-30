using Altair8800Emulator;

namespace Altair8800EmulatorTests
{
    public class Tests
    {
        public Machine TestMachine = null;

        [SetUp]
        public void Setup()
        {
            TestMachine= new Machine();
        }

        /// <summary>
        /// Run the test program from Part 3B of the Altair manual.
        /// </summary>
        [Test(Author ="John Brewer",Description ="Part 3B Addition Test")]
        public void Manual3BAdditionTest()
        {
            byte[] TestProgram = new byte[]
            {
                0b00111010, //LDA
                0b10000000,
                0b00000000,
                0b01000111, //MOV
                0b00111010, //LDA
                0b10000001,
                0b00000000,
                0b10000000, //ADD
                0b00110010, //STA
                0b10000010,
                0b00000000,
                0b11000011, //JMP
                0b00000000,
                0b00000000
            };

            TestMachine.LoadCode(TestProgram);

            TestMachine.MainMemory[0x80] = 4;
            TestMachine.MainMemory[0x81] = 2;


            for (int x =0;x<6;x++)
            {
                TestMachine.Step();
            }

            Assert.That(TestMachine.MainMemory[0x82],Is.EqualTo(6));
            Assert.That(TestMachine.ProgramCounter,Is.EqualTo(0));


            Assert.Pass();
        }

        [Test(Author = "John Brewer",Description = "Check Flags after carrying ADD operation.")]
        public void ADDInstructionFlagTests()
        {
            //Modification of the 3B test program
            byte[] TestProgram = new byte[]
            {
                0b00111010, //LDA
                0b10000000,
                0b00000000,
                0b01000111, //MOV
                0b00111010, //LDA
                0b10000001,
                0b00000000,
                0b10000000, //ADD
            };

            TestMachine.LoadCode(TestProgram);

            //Set values to ones that will carry
            TestMachine.MainMemory[0x80] = 128;
            TestMachine.MainMemory[0x81] = 129;

            TestMachine.Step(); // Load to Accumulator from 0x80
            TestMachine.Step(); // MOV Accumulator to Register B
            TestMachine.Step(); // Load to Accumulator from 0x81
            TestMachine.Step(); // ADD Register B to Accumulator

            //Verify that the Carry Bit was set.
            Assert.That(TestMachine.CarryBit, Is.True,"Carry bit was not set on a carrying add operation (Instruction 0x80)");
            Assert.That(TestMachine.ZeroBit, Is.False, "Zero bit was set on a carrying add operation (Instruction 0x80)");
            Assert.That(TestMachine.SignBit, Is.False, "Sign bit was set on a carrying add operation (Instruction 0x80)");
            Assert.That(TestMachine.AuxBit, Is.False, "Aux bit was set on a carrying add operation (Instruction 0x80)");
            Assert.That(TestMachine.ParityBit, Is.True, "Parity bit was not set on a carrying add operation (Instruction 0x80)");

            Assert.Pass();
        }

        [Test(Author = "John Brewer", Description = "Check Flags after negative SUB operation.")]
        public void SUBNegativeInstructionFlagTests()
        {
            //Modification of the 3B test program
            byte[] TestProgram = new byte[]
            {
                0b00111010, //LDA
                0b10000000,
                0b00000000,
                0b01000111, //MOV
                0b00111010, //LDA
                0b10000001,
                0b00000000,
                0b10010000, //SUB
            };

            TestMachine.LoadCode(TestProgram);

            //Set values to ones that will carry
            TestMachine.MainMemory[0x80] = 19;
            TestMachine.MainMemory[0x81] = 18;

            TestMachine.Step(); // Load to Accumulator from 0x80
            TestMachine.Step(); // MOV Accumulator to Register B
            TestMachine.Step(); // Load to Accumulator from 0x81
            TestMachine.Step(); // SUB Register B to Accumulator

            //Verify status bits are what they should be
            Assert.That(TestMachine.CarryBit, Is.False, "Carry bit was set on a carrying add operation (Instruction 0x80)");
            Assert.That(TestMachine.ZeroBit, Is.False, "Zero bit was set on a carrying add operation (Instruction 0x80)");
            Assert.That(TestMachine.SignBit, Is.True, "Sign bit was not set on a carrying add operation (Instruction 0x80)");
            Assert.That(TestMachine.AuxBit, Is.False, "Aux bit was set on a carrying add operation (Instruction 0x80)");
            Assert.That(TestMachine.ParityBit, Is.False, "Parity bit was set on a carrying add operation (Instruction 0x80)");

            Assert.Pass();
        }

        [Test(Author = "John Brewer", Description = "Check Flags after zero SUB operation.")]
        public void SUBZeroInstructionFlagTests()
        {
            //Modification of the 3B test program
            byte[] TestProgram = new byte[]
            {
                0b00111010, //LDA
                0b10000000,
                0b00000000,
                0b01000111, //MOV
                0b00111010, //LDA
                0b10000001,
                0b00000000,
                0b10010000, //SUB
            };

            TestMachine.LoadCode(TestProgram);

            //Set values to ones that will carry
            TestMachine.MainMemory[0x80] = 18;
            TestMachine.MainMemory[0x81] = 18;

            TestMachine.Step(); // Load to Accumulator from 0x80
            TestMachine.Step(); // MOV Accumulator to Register B
            TestMachine.Step(); // Load to Accumulator from 0x81
            TestMachine.Step(); // SUB Register B from Accumulator

            //Verify status bits are what they should be
            Assert.That(TestMachine.CarryBit, Is.True, "Carry bit was not set on a carrying add operation (Instruction 0x80)");
            Assert.That(TestMachine.ZeroBit, Is.True, "Zero bit was not set on a carrying add operation (Instruction 0x80)");
            Assert.That(TestMachine.SignBit, Is.False, "Sign bit was set on a carrying add operation (Instruction 0x80)");
            Assert.That(TestMachine.AuxBit, Is.False, "Aux bit was set on a carrying add operation (Instruction 0x80)");
            Assert.That(TestMachine.ParityBit, Is.False, "Parity bit was set on a carrying add operation (Instruction 0x80)");

            Assert.Pass();
        }
    }
}
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
    }
}
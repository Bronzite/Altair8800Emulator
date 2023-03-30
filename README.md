# Altair 8800 Emulator

Basic Altair 8800 Emulator based on the 8080 Instruction Set.  This is a 
library intended to allow users to easily simulate an Altair 8800 system using
a small set of easily-understood C# objects.

## Usage

Create a Machine using the default constructor:

``` 
Machine testMachine = new Machine(); 
```

Then load the disk files:

``` 
 byte[] bDiskFile = File.ReadAllBytes("program.dsk");
 testMachine.LoadCode(bDiskFile);
```

Initiate run:

``` 
while(true)
{
      testMachine.Step();
}
```

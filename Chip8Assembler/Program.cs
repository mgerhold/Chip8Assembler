using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

// inspiration: https://github.com/craigthomas/Chip8Assembler
class Chip8Assembler
{
    private List<byte> bytecode = new List<byte>();

    private enum LineType { Label, Mnemonic };
    private enum ParameterType { Hex, Register, Label };
    private enum LabelWordMode { None, Word, AfterWord };

    private struct Parameter
    {
        public ParameterType Type;
        public ushort HexValue;
        public byte RegisterValue;
        public string LabelValue;

        public Parameter(ushort hexValue)
        {
            Type = ParameterType.Hex;
            this.HexValue = hexValue;
            RegisterValue = 0;
            LabelValue = "";
        }

        public Parameter(byte registerValue)
        {
            Type = ParameterType.Register;
            HexValue = 0;
            this.RegisterValue = registerValue;
            LabelValue = "";
        }

        public Parameter(string labelValue)
        {
            Type = ParameterType.Label;
            HexValue = 0;
            RegisterValue = 0;
            this.LabelValue = labelValue;
        }
    }
    private struct Mnemonic
    {
        public string Name;
        public string Opcode;

        public Mnemonic(string name, string opcode)
        {
            Name = name;
            Opcode = opcode;
        }

        public int GetOperandsCount()
        {
            int result = 0;
            if (Opcode.Contains("n"))
                result++;
            if (Opcode.Contains("s"))
                result++;
            if (Opcode.Contains("t"))
                result++;
            return result;
        }

        public int GetNLength()
        {
            int result = 0;
            for (int i = 0; i < Opcode.Length; i++)
                if (Opcode[i] == 'n')
                    result++;
            return result;
        }
    }

    private static readonly List<Mnemonic> mnenomics = new List<Mnemonic>() {
        new Mnemonic("SYS", "0nnn"),
        new Mnemonic("CLR", "00E0"),
        new Mnemonic("RTS", "00EE"),
        new Mnemonic("JUMP", "1nnn"),
        new Mnemonic("CALL", "2nnn"),
        new Mnemonic("SKE", "3snn"),
        new Mnemonic("SKNE", "4snn"),
        new Mnemonic("SKRE", "5st0"),
        new Mnemonic("LOAD", "6snn"),
        new Mnemonic("ADD", "7snn"),
        new Mnemonic("MOVE", "8st0"),
        new Mnemonic("OR", "8st1"),
        new Mnemonic("AND", "8st2"),
        new Mnemonic("XOR", "8st3"),
        new Mnemonic("ADDR", "8st4"),
        new Mnemonic("SUB", "8st5"),
        new Mnemonic("SHR", "8st6"),
        new Mnemonic("SHL", "8stE"),
        new Mnemonic("SKRNE", "9st0"),
        new Mnemonic("LOADI", "Annn"),
        new Mnemonic("JUMPI", "Bnnn"),
        new Mnemonic("RAND", "Ctnn"),
        new Mnemonic("DRAW", "Dstn"),
        new Mnemonic("SKPR", "Es9E"),
        new Mnemonic("SKUP", "EsA1"),
        new Mnemonic("MOVED", "Ft07"),
        new Mnemonic("KEYD", "Ft0A"),
        new Mnemonic("LOADD", "Fs15"),
        new Mnemonic("LOADS", "Fs18"),
        new Mnemonic("ADDI", "Fs1E"),
        new Mnemonic("LDSPR", "Fs29"),
        new Mnemonic("BCD", "Fs33"),
        new Mnemonic("STOR", "Fs55"),
        new Mnemonic("READ", "Fs65"),
    };

    private static bool TranslateMnemonic(string line, Dictionary<string, ushort> labelAddresses, out byte[] bytecode, out string error)
    {
        error = "";
#if DEBUG
        Console.WriteLine("Translating line: {0}", line);
#endif
        bytecode = new byte[2];
        line = line.Trim();
        string[] parts = line.Split(null); // split at whitespaces
        string symbol = parts[0];
        string arguments = string.Join("", parts.Skip(1));
        string[] operands = arguments == "" ? new string[0] : arguments.Split(',');
#if DEBUG
        Console.WriteLine("\tSymbol: {0}", symbol);
#endif
        for (int i = 0; i < operands.Length; i++)
        {
            operands[i] = operands[i].Trim();
#if DEBUG
            Console.WriteLine("\t\tOperand: {0}", operands[i]);
#endif
        }
        bool found = false;
        Mnemonic mnemonic = new Mnemonic();
        foreach (Mnemonic m in mnenomics)
        {
            if (m.Name == symbol)
            {
                found = true;
                mnemonic = m;
                break;
            }
        }
        if (!found)
        {
            error = String.Format("Unknown mnemonic: {0}", symbol);
            return false;
        }
        if (operands.Length != mnemonic.GetOperandsCount())
        {
            error = String.Format("Invalid number of operands for mnemonic {0}: Expected {1}, got {2}.",
                symbol, mnemonic.GetOperandsCount(), operands.Length);
            return false;
        }
        List<Parameter> parameterList = StringArrayToParameterList(operands);
        int index = 0;
        string opcode = mnemonic.Opcode;
        while (index < mnemonic.GetOperandsCount())
        {
            if (opcode.Contains("s"))
            {
                if (parameterList[index].Type != ParameterType.Register)
                {
                    error = String.Format("Invalid parameter type for mnemonic {0} (parameter {1}): Expected register.", mnemonic.Name, index + 1);
                    return false;
                }
                opcode = opcode.Replace("s", String.Format("{0:X1}", parameterList[index].RegisterValue));
                index++;
            }
            else if (opcode.Contains("t"))
            {
                if (parameterList[index].Type != ParameterType.Register)
                {
                    error = String.Format("Invalid parameter type for mnemonic {0} (parameter {1}): Expected register.", mnemonic.Name, index + 1);
                    return false;
                }
                opcode = opcode.Replace("t", String.Format("{0:X1}", parameterList[index].RegisterValue));
                index++;
            }
            else if (opcode.Contains("nnn"))
            {
                if (parameterList[index].Type == ParameterType.Hex)
                    opcode = opcode.Replace("nnn", String.Format("{0:X3}", parameterList[index].HexValue));
                else if (parameterList[index].Type == ParameterType.Label)
                    opcode = opcode.Replace("nnn", String.Format("{0:X3}", labelAddresses[parameterList[index].LabelValue]));
                else
                {
                    error = String.Format("Inavlid address or label for mnemonic {0}.", mnemonic.Name);
                    return false;
                }
                index++;
            }
            else if (opcode.Contains("nn"))
            {
                if (parameterList[index].Type != ParameterType.Hex)
                {
                    error = String.Format("Invalid parameter type for mnemonic {0} (parameter {1}): Expected a hex value.", mnemonic.Name, index + 1);
                    return false;
                }
                opcode = opcode.Replace("nn", String.Format("{0:X2}", parameterList[index].HexValue));
                index++;
            }
            else if (opcode.Contains("n"))
            {
                if (parameterList[index].Type != ParameterType.Hex)
                {
                    error = String.Format("Invalid parameter type for mnemonic {0} (parameter {1}): Expected a hex value.", mnemonic.Name, index + 1);
                    return false;
                }
                opcode = opcode.Replace("n", String.Format("{0:X1}", parameterList[index].HexValue));
                index++;
            }
        }
#if DEBUG
        Console.WriteLine("Generating opcode: {0}", opcode);
#endif
        bytecode[0] = (byte)Convert.ToInt16(opcode.Substring(0, 2), 16);
        bytecode[1] = (byte)Convert.ToInt16(opcode.Substring(2, 2), 16);
        return true;
    }

    private static List<Parameter> StringArrayToParameterList(string[] operands)
    {
        List<Parameter> result = new List<Parameter>();
        foreach (string operand in operands)
        {
            if (operand[0] == '$')
            { // hex value
                result.Add(new Parameter((ushort)Convert.ToInt16(operand.Substring(1), 16)));
#if DEBUG
                Console.WriteLine("Creating parameter: 0x{0:X4}", result.Last().HexValue);
#endif
            }
            else if (operand[0] == 'V')
            { // register
                result.Add(new Parameter((byte)Convert.ToInt16(operand.Substring(1), 16)));
#if DEBUG
                Console.WriteLine("Creating parameter: V{0:X}", result.Last().RegisterValue);
#endif
            }
            else if (Char.IsLetter(operand[0]))
            { // label
                result.Add(new Parameter(operand));
#if DEBUG
                Console.WriteLine("Creating parameter: {0} (label)", result.Last().LabelValue);
#endif
            }
        }
        return result;
    }

    public static bool AssembleFromFile(string filename, string destFilename, out string error)
    {
        string[] lines;
        List<byte> result = new List<byte>();
        try
        {
            lines = File.ReadAllLines(filename);
        }
        catch (Exception e)
        {
            error = e.Message;
            return false;
        }

        RemoveComments(ref lines);
        TrimLines(ref lines);
        ushort address = 0x200;
        Dictionary<string, ushort> labelAddresses = new Dictionary<string, ushort>();
        // fetch all labels
        foreach (string line in lines)
        {
            string label;
            bool isLabel = IsLabel(line, out label);
            //Console.Write("0x{0:X4} ", address);
            if (IsLabel(line, out label))
            {
#if DEBUG
                Console.WriteLine("[LABEL] {0}", label);
#endif
                if (labelAddresses.ContainsKey(label))
                {
                    error = "Duplicate label: " + label;
                    return false;
                }
                labelAddresses[label] = address;
            }
            else
            {
                //TranslateMnemonic(line, out _);
                address += 2;
            }
        }

        // output label addresses
#if DEBUG
        Console.WriteLine("\nLabel addresses:");
        foreach (KeyValuePair<string, ushort> labelAddress in labelAddresses)
        {
            Console.WriteLine("{0}\t\t0x{1:x4}", labelAddress.Key, labelAddress.Value);
        }
        Console.WriteLine();
#endif

        // translate
        foreach (string line in lines)
        {
            string label;
            bool isLabel = IsLabel(line, out label);
#if DEBUG
            Console.Write("0x{0:X4} ", address);
#endif
            if (IsLabel(line, out label))
            {
#if DEBUG
                Console.WriteLine("[LABEL] {0}", label);
#endif
            }
            else
            {
                byte[] bytecode;
                string translationError;
                if (!TranslateMnemonic(line, labelAddresses, out bytecode, out translationError))
                {
                    error = translationError;
                    return false;
                }
                result.Add(bytecode[0]);
                result.Add(bytecode[1]);
#if DEBUG
                Console.Write("  ==>  ");
                foreach (byte b in bytecode)
                    Console.Write("{0:X2}", b);
                Console.WriteLine();
#endif
                address += 2;
            }
        }

        try
        {
            File.WriteAllBytes(destFilename, result.ToArray());
        }
        catch (Exception e)
        {
            error = "Error while writing output file: " + e.Message;
            return false;
        }

        error = "";
        return true;
    }

    private static void RemoveComments(ref string[] lines)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            string[] parts = lines[i].Split('#');
            lines[i] = parts[0];
        }
    }

    private static void TrimLines(ref string[] lines)
    {
        List<string> result = new List<string>();
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Trim() != "")
                result.Add(lines[i].Trim());
        }
        lines = result.ToArray();
    }

    private static bool IsLabel(string line, out string label)
    {
        LabelWordMode mode = LabelWordMode.None;
        line = line.Trim();
        label = "";
        if (line[line.Length - 1] != ':')
            return false;
        int pos = 0;
        while (pos < line.Length)
        {
            switch (mode)
            {
                case LabelWordMode.None:
                    if (Char.IsLetter(line[pos]) && line[pos] != 'V')
                    {
                        mode = LabelWordMode.Word;
                        label += line[pos];
                    }
                    else
                        return false;
                    break;
                case LabelWordMode.Word:
                    if (Char.IsLetterOrDigit(line[pos]))
                    {
                        label += line[pos];
                    }
                    else if (line[pos] == ':')
                    {
                        return true;
                    }
                    else
                    {
                        mode = LabelWordMode.AfterWord;
                    }
                    break;
                case LabelWordMode.AfterWord:
                    if (line[pos] == ':')
                        return true;
                    else
                        return false;
            }
            pos++;
        }
        return false;
    }
}

class Program
{
    static int Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Please specify a source file as argument.");
            return 1;
        }
        foreach (string filename in args)
        {
            Console.Write("Creating CHIP-8 byte code from {0}...", filename);
            string error;
            if (!Chip8Assembler.AssembleFromFile(filename, Path.ChangeExtension(filename, "ch8"), out error))
            {
                Console.WriteLine("FAILURE!\n{0}\nAborting...", error);
                return 1;
            }
            Console.WriteLine("SUCCESS!");
        }
        return 0;
    }
}
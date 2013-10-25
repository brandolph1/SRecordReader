using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using WelchAllyn.SRecord;
using WelchAllyn.Nand512Library;

namespace WelchAllyn.SRecordReader
{
    enum Endian
    {
        Little,
        Big
    };

    class Program
    {
        [STAThreadAttribute]
        static void Main(string[] args)
        {
            Endian endian = Endian.Little;
            CSRecord srec;
            string strInFile = null;
            FileStream fs_in = null;

            if (0 < args.Length)    // Is a file name specified on the command line?
            {
                strInFile = args[0];
            }
            else
            {
                OpenFileDialog ofd = new OpenFileDialog();

                ofd.CheckFileExists = true;
                ofd.CheckPathExists = true;
                ofd.DefaultExt = "*.S19";
                ofd.Filter = "S19 files (*.S19)|*.S19|All files (*.*)|*.*";
                ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                ofd.Multiselect = false;
                ofd.ShowHelp = false;
                ofd.Title = "Open an S-Record Image File";

                if (DialogResult.OK == ofd.ShowDialog())
                {
                    strInFile = ofd.FileName;
                }

                ofd.Dispose();
            }

            if ((null != strInFile) && (0 < strInFile.Length))
            {
                try
                {
#if DEBUG
                    Console.WriteLine("Opening input file: {0}", strInFile);
#endif
                    fs_in = new FileStream(strInFile, FileMode.Open, FileAccess.Read);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message.ToString());
                }

                if (null != fs_in)
                {
                    StreamReader str_in = new StreamReader(fs_in.Name);
                    String strInLine;
                    Byte[] data;
                    Byte[] memory;
                    uint chksum = 0;
                    int state;
                    int line;
                    uint start_addr, next_expected_start_addr;
                    const int sizeof_NAND512 = 0x4000000;   // NAND512 holds 64MB of data

                    try
                    {
                        memory = new Byte[sizeof_NAND512];
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Failed to allocated memory array, {0}", ex.ToString());
                        return;
                    }

                    for (int ii = 0; ii < sizeof_NAND512; ++ii)
                    {
                        memory[ii] = 0xFF;
                    }

                    Console.WriteLine("Filling memory...");
                    line = 0;
                    start_addr = 0U;
                    next_expected_start_addr = 0xFFFFFFF0U;

                    while ((strInLine = str_in.ReadLine()) != null)
                    {
                        ++line;
                        srec = new CSRecord(strInLine);
                        data = srec.DataBytes;

                        start_addr = srec.MemoryAddress;

                        if (start_addr != next_expected_start_addr)
                        {
                            if (srec.RecordType != (int)CSRecord.SRecordType.S7)
                            {
                                Console.WriteLine("Gap in image found at line {0}, start={1:X8}, expected={2:X8}", line, start_addr, next_expected_start_addr);
                            }
                        }

                        for (int ii = 0; ii < data.Length; ++ii)
                        {
                            memory[start_addr + ii] = data[ii];
                        }

                        next_expected_start_addr = start_addr + (uint)data.Length;
                    }
#if True==True
                    CDevice device = new CDevice();
                    byte[] block = new byte[device.BlockLength];
                    uint other = 0;

                    state = 0;

                    for (int ii = 0; ii < memory.Length; ++ii)
                    {
                        other += (uint)memory[ii];

                        switch (state)
                        {
                            case 0:
                                if (endian == Endian.Little)
                                {
                                    chksum += (uint)memory[ii];
                                }
                                else
                                {
                                    chksum += ((uint)memory[ii] * 0x100U);
                                }
                                state = 1;
                                break;
                            case 1:
                                if (endian == Endian.Little)
                                {
                                    chksum += ((uint)memory[ii] * 0x100U);
                                }
                                else
                                {
                                    chksum += (uint)memory[ii];
                                }
                                state = 0;
                                break;
                            default:
                                throw new ApplicationException("Unexpected state value");
                        }
                    }
#endif
                    str_in.Close();
                    Console.WriteLine("16 bit Checksum={0:X8}, other={1:X8}", chksum, other);
                }
#if DEBUG
                //MessageBox.Show("Done","SRecord Reader...");
                Console.WriteLine("Done, press <enter> to close window.");
                Console.ReadLine();
#endif
            }
        }
    }
}

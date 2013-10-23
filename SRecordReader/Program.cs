using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using WelchAllyn.SRecord;

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
            Endian endian = Endian.Big;
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
                    uint chksum = 0;
                    int state;
                    int line;
                    uint last_start_addr, next_expected_start_addr;

                    Console.WriteLine("Scanning...");
                    state = 0;
                    line = 1;
                    last_start_addr = 0U;
                    next_expected_start_addr = 0xFFFFFFF0U;

                    while ((strInLine = str_in.ReadLine()) != null)
                    {
                        srec = new CSRecord(strInLine);
                        data = srec.DataBytes;

                        if (state != 0)
                        {
                            Console.WriteLine("Unexpected--starting new hex-line at state 1");
                        }

                        for (int ii = 0; ii < data.Length; ++ii)
                        {
                            switch (state)
                            {
                                case 0:
                                    if (endian == Endian.Little)
                                    {
                                        chksum += (uint)data[ii];
                                    }
                                    else
                                    {
                                        chksum += ((uint)data[ii] * 0x100);
                                    }
                                    state = 1;
                                    break;
                                case 1:
                                    if (endian == Endian.Little)
                                    {
                                        chksum += ((uint)data[ii] * 0x100);
                                    }
                                    else
                                    {
                                        chksum += (uint)data[ii];
                                    }
                                    state = 0;
                                    break;
                                default:
                                    throw new ApplicationException("Unexpected state value");
                            }
                        }
                    }

                    str_in.Close();
                    Console.WriteLine("16 bit Checksum={0:X8}", chksum);
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

using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using WelchAllyn.SRecord;

namespace WelchAllyn.SRecordReader
{
    class Program
    {
        [STAThreadAttribute]
        static void Main(string[] args)
        {
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
                    ulong chksum = 0UL;
                    int state;

                    Console.WriteLine("Scanning...");
                    state = 0;

                    while ((strInLine = str_in.ReadLine()) != null)
                    {
                        srec = new CSRecord(strInLine);
                        data = srec.DataBytes;

                        for (int ii = 0; ii < data.Length; ++ii)
                        {
                            switch (state)
                            {
                                case 0:
                                    chksum += (ulong) data[ii];
                                    state = 1;
                                    break;
                                case 1:
                                    chksum += ((ulong)data[ii] * 0x100UL);
                                    state = 0;
                                    break;
                                default:
                                    throw new ApplicationException("Unexpected state value");
                            }
                        }
                    }

                    str_in.Close();
                    //fs_in.Close();
                    Console.WriteLine("16 bit Checksum={0:X}", chksum);
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

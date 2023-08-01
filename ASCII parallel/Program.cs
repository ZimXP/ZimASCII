using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace ASCII_3._0
{
    internal class Program
    {
        public static void DrawText(String text, Font font, Color textColor, int maxWidth, String path)
        {
            //first, create a dummy bitmap just to get a graphics object
            Image img = new Bitmap(1, 1);
            Graphics drawing = Graphics.FromImage(img);
            //measure the string to see how big the image needs to be
            SizeF textSize = drawing.MeasureString(text, font, maxWidth);

            //set the stringformat flags to rtl
            StringFormat sf = new StringFormat();
            sf.Trimming = StringTrimming.Word;

            //free up the dummy image and old graphics object
            img.Dispose();
            drawing.Dispose();

            //create a new image of the right size
            img = new Bitmap((int)textSize.Width, (int)textSize.Height);
            drawing = Graphics.FromImage(img);
            //paint the background
            drawing.Clear(Color.Black);

            //create a brush for the text
            Brush textBrush = new SolidBrush(textColor);

            drawing.DrawString(text, font, textBrush, new RectangleF(0, 0, textSize.Width, textSize.Height), sf);

            drawing.Save();

            img.Save(path, ImageFormat.Jpeg);
            img.Dispose();
            drawing.Flush();
            GC.Collect();

        }

        //bat_command will be used to execute shell commands
        static void bat_command(string command)
        {
            //File.WriteAllText("command.bat", "ffmpeg.exe " + command + "\npause");
            File.WriteAllText("command.bat", "ffmpeg.exe " + command + "\n");
            var bat = Process.Start("command.bat");
            bat.WaitForExit();
        }
        static void Main(string[] args)
        {
            Console.CursorVisible = false;
            GC.Collect();
            Console.Title = "ZimASCII 1.1.3";
            //Establishes the names of the folders
            string[] folders = { 
                "Output\\",             //0
                "Input\\",              //1
                "Temp\\",               //2
                "Temp\\Scale\\",        //3
                "Temp\\ASCII\\",        //4
                "Temp\\Original\\" };   //5
            bool folder_exist = true;
            if (Directory.Exists(folders[2])) { 
                Directory.Delete(folders[2], true);
            }

            //Makes sure that the input folder exists, because the program relies on its contents
            if (!Directory.Exists(folders[1]))
            {
                folder_exist = false;
            }

            //Goes through the folders array and creates the required folders if they are missing
            for (int i = 0; i < folders.Length; i++)
            {
                if (!Directory.Exists(folders[i]))
                {
                    Directory.CreateDirectory(folders[i]);
                }
            }
            string config = "config.cfg";


            //Establishes default config values
            string[] configtext =
              { "fps = ",
                "width (amount of horizontal characters) = ",
                "font width (affects the scale of the characters themselves, which affects the image size) = ",
                "threads (exceeding 4 can cause the program to consume a ludicrous amount of memory) = "
              };

            string[] config_def =
            {
                configtext[0].Replace(configtext[0], configtext[0] + "30"),
                configtext[1].Replace(configtext[1], configtext[1] + "100"),
                configtext[2].Replace(configtext[2], configtext[2] + "6"),
                configtext[3].Replace(configtext[3], configtext[3] + "4")
            };

            //If the a folder was missing before the program started the program will shut down
            if (!File.Exists(config) || !folder_exist)
            {
                File.WriteAllText(config, string.Join("\n", config_def));
                Console.WriteLine("One or more files are missing, press enter to close the program");
                Console.ReadLine();
                Environment.Exit(1);
            }

            //Gets the first file from the Input folder and converts it into a temporary jpg so that the program can get the resolution 
            string[] input = Directory.GetFiles(folders[1]);
            string temp = folders[3] + "scale.png";
            Console.WriteLine(input[0]);
            bat_command(string.Format("-i \"{0}\" {1}", input[0], temp));
            Image image = Image.FromFile(temp);

            //Gets the resolution from the temporary image and then disposes of it
            double horz = image.Width, vert = image.Height;
            double mult = (vert / horz) * .5;
            double mult2 = (vert / horz);
            image.Dispose();


            //Reads config file and converts values into variables
            string[] configs = File.ReadAllLines(config);
            int fps = int.Parse(configs[0].Replace(configtext[0], ""));
            int width = int.Parse(configs[1].Replace(configtext[1], "")); ;
            int fontwidth = int.Parse(configs[2].Replace(configtext[2], ""));
            int threads = int.Parse(configs[3].Replace(configtext[3], ""));


            //Converts the original image resolutions into the appropriate ASCII equivalent
            Font consolas = new Font("consolas", fontwidth * 2);
            int height = Convert.ToInt32(width * mult);

            //Get the extention type of the file to tell whether it's a video or an image
            string extype = Path.GetExtension(input[0]);
            Console.WriteLine(extype);

            //the program checks if the file has any of these extention types, if it doesn't then it assumes the file is a video
            bool isimage = false;
            if (extype == ".jpg" || extype == ".bmp" || extype == ".png" || extype == ".webp" || extype == ".jpeg")
            {
                Console.WriteLine("File is an image");
                bat_command(string.Format("-i {0} -vf scale={1}:{2} {3}", input[0], width, height, temp));
                isimage = true;
            }
            else
            {
                Console.WriteLine("File is a video");
                temp = folders[5] + "out_img%%09d.png";
                bat_command(string.Format("-i \"{0}\" -r {4} -vf scale={1}:{2} {3}", input[0], width, height, temp, fps));
                bat_command(string.Format("-i \"{0}\" -f wav Temp\\out.wav", input[0]));
            }

            Console.Clear();
            List<string> imgs = new List<string>();
            for (int i = 0; i < Directory.GetFiles(folders[5]).Length; i++)
            {
                if (Directory.GetFiles(folders[5])[i].Contains("out_img"))
                {
                    imgs.Add(Directory.GetFiles(folders[5])[i]);
                    Console.SetCursorPosition(0, 0);
                    double frame = Directory.GetFiles(folders[5]).Length;
                    double percent = Math.Round((i / frame) * 100);
                    Console.WriteLine("Indexing frames");
                    Console.WriteLine("Frame {0}/{1}\n{2}%", i, frame, percent);
                    //Console.WriteLine(imgs[i]);
                }
            }
            
            //Gets the amount of frames within the image
            double frameCount = imgs.Count;


            //makes the character array
            char[] chars = { ' ', ' ', '.', ',', ':', '=', '+', '*', '#', '%', '@' };
            //Array.Reverse(chars);
            Console.CursorVisible = false;
            //Starting the conversion from image to ASCII
            List<string> ascii_ini = new List<string>();
            Console.Clear();


            Parallel.For(0, int.Parse(frameCount.ToString()), new ParallelOptions { MaxDegreeOfParallelism = threads * 2 }, i =>
            {
                ascii_ini.Add("");
                Console.SetCursorPosition(0, 0);
                double percent = Math.Round((i / frameCount) * 100);
                //Console.WriteLine("Frame {0}/{1}\n{2}%", i, frameCount, percent);
            });



            string[] ascii = ascii_ini.ToArray();

            Console.Clear();

            int cycles = 0;
            Console.WriteLine("Generating ASCII art, this process can be so fast that you won't even get to read this whole message.");
            Parallel.For(0, ascii.Length, new ParallelOptions { MaxDegreeOfParallelism = threads * 2 }, i =>
            {
                {
                    StringBuilder sb = new StringBuilder();
                    Image img = Image.FromFile(imgs[i]);
                    //selects the frame from the image represented by the "i"
                    Console.SetCursorPosition(0, 0);
                    //goes through each vertical row in the image
                    for (int h = 0; h < img.Height; h++)
                    {
                        sb.Append("|");
                        //goes through each pixel in the row
                        for (int w = 0; w < img.Width; w++)
                        {
                            //determines the brightnes of the pixel and converts it into the appropriate character from the chars array
                            Color cl = ((Bitmap)img).GetPixel(w, h);
                            int gray = (cl.R + cl.G + cl.B) / 3;
                            int index = gray * (chars.Length - 1) / 255;

                            //writes the character into the stringbuilder
                            sb.Append(chars[index]);
                            //Console.Write(chars[index]);
                        }
                        sb.Append("|");
                        sb.Append('\n');
                        //Console.Write("\n");

                    }
                    //ascii()(sb.ToString());
                    //Console.SetCursorPosition(0, 0);
                    ascii[i] = sb.ToString();
                    //calculate the percentage and draws the progress to the progress bar
                    //double percent = Math.Round((i / frameCount) * 100);
                    //Console.Clear();

                    //Console.Write(sb.ToString());
                    //Console.WriteLine("Converting frames");
                    //Console.WriteLine("Frame {0}/{1}\n{2}%", i, frameCount, percent);
                    //File.WriteAllText("thing.txt", sb.ToString());
                    sb.Clear();

                    GC.Collect();
                    //Console.SetCursorPosition(0, 0);

                }
            });

            Console.Clear();
            double i_d = 0;
            int u = 0;
            Parallel.For(0, ascii.Length, new ParallelOptions { MaxDegreeOfParallelism = threads }, e =>
            {
                Console.SetCursorPosition(0, 0);

                //Draw the string to an image file
                DrawText(ascii[e], consolas, Color.FromArgb(255, 255, 255), 30000, folders[4] + e + ".jpg");
                Console.SetCursorPosition(0, 0);
                i_d += 1;
                Console.WriteLine("\rConverting to image sequence " + i_d + "/" + frameCount);
                double percent = Math.Round((u / frameCount) * 100);
                Console.WriteLine("Frame {0}/{1}\n{2}%", u, frameCount, percent);
                u++;
                Console.SetCursorPosition(0, 0);
                GC.Collect();
                image.Dispose();
            });

            image.Dispose();
            Console.Clear();
            //establishes the output resolution
            image = Image.FromFile(folders[4] + 0 + ".jpg");
            width = image.Width;
            if (width % 2 != 0)
            {
                width += 1;
            }
            height = image.Height;
            if (height % 2 != 0)
            {
                height += 1;
            }
            image.Dispose();

            Console.CursorVisible = true;
            //If the original file is a video the program combines all the images into an mp4
            //otherwise the program assumes that the original file was an image and just copies the new image into the Output folder
            string filename = Path.GetFileNameWithoutExtension(input[0]);
            if (!isimage)
            {
                bat_command("-framerate " + fps + @" -i " + folders[4] + "%%d.jpg -s " + width + "x" + height + " Temp\\outputs.mp4");

                if (File.Exists("Temp\\out.wav"))
                {
                    bat_command("-i Temp\\outputs.mp4 -i Temp\\out.wav \"Output\\" + filename + "_ZimASCII.mp4\"");
                }
                else
                {
                    if (File.Exists("Output\\" + filename + "_ZimASCII.mp4"))
                    {
                        Console.WriteLine(filename + ".jpeg already exists, would you like to replace it? y/N");
                        switch (Console.ReadLine().Replace(" ", ""))
                        {
                            case "Y":
                            case "y":
                                File.Delete("Output\\" + filename + "_ZimASCII.mp4");
                                File.Copy("Temp\\outputs.mp4", "Output\\" + filename + "_ZimASCII.mp4");
                                break;
                        }
                    }
                    else
                    {
                        File.Copy("Temp\\outputs.mp4", "Output\\" + filename + "_ZimASCII.mp4");
                    }
                }
            }
            else
            {
                if (File.Exists("Output\\" + filename + ".jpg"))
                {
                    Console.WriteLine(filename + "_ZimASCII.jpeg already exists, would you like to replace it? y/N");
                    switch (Console.ReadLine().Replace(" ", ""))
                    {
                        case "Y":
                        case "y":
                            File.Delete("Output\\" + filename + ".jpg");
                            File.Copy("Temp\\0.jpg", "Output\\" + filename + "_ZimASCII.jpg");
                            break;
                    }
                }
                else
                {
                    File.Copy("Temp\\0.jpg", "Output\\" + filename + "_ZimASCII.jpg");
                }
            }
            File.Delete("command.bat");
            Environment.Exit(1);
        }
    }
}

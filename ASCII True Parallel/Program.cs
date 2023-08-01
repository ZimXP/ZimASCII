using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Image = System.Drawing.Image;

namespace ASCII_Rewrite
{
    class Program
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
            File.WriteAllText("command.bat", "ffmpeg.exe " + command + "\npause");
            var bat = Process.Start("command.bat");
            bat.WaitForExit();
        }
        static void Main(string[] args)
        {
            GC.Collect();
            Console.Title = "ZimASCII 2";

            //Establishes the names of the folders
            string[] folders = { "Output\\", "Input\\", "Temp\\", "Temp\\Temp2\\" };
            bool folder_exist = true;

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
            //If the a folder was missing before the program started the program will shut down
            if (!File.Exists(config) || !folder_exist)
            {
                File.WriteAllText(config, "fps = 30\nwidth (amount of horizontal characters) = 100\nfont width (affects the scale of the characters themselves, which affects the image size) = 6");
                Console.WriteLine("One or more files are missing, press enter to close the program");
                Console.ReadLine();
                Environment.Exit(1);
            }

            //Gets the first file from the Input folder and converts it into a temporary jpg so that the program can get the resolution 
            string[] input = Directory.GetFiles(folders[1]);
            string temp = "Temp\\out.png";
            Console.WriteLine(input[0]);
            bat_command(string.Format("-i \"{0}\" {1}", input[0], temp));
            Image image = Image.FromFile(temp);

            //Gets the resolution from the temporary image and then disposes of it
            double horz = image.Width, vert = image.Height;
            double mult = (vert / horz) * .5;
            double mult2 = (vert / horz);
            image.Dispose();
            File.Delete(temp);

            //Converts the original image resolutions into the appropriate ASCII equivalent
            string[] configs = File.ReadAllLines(config);
            int fps = int.Parse(configs[0].Replace("fps = ", ""));
            int width = int.Parse(configs[1].Replace("width (amount of horizontal characters) = ", "")); ;
            int height = Convert.ToInt32(width * mult);

            //Get the extention type of the file to tell whether it's a video or an image
            string extype = Path.GetExtension(input[0]);
            Console.WriteLine(extype);
            //the program checks if the file has any of these extention types, if it doesn't then it assumes the file is a video
            bool isimage = false;
            if (extype == ".jpg" || extype == ".bmp" || extype == ".jpg" || extype == ".webp" || extype == ".jpeg")
            {
                Console.WriteLine("File is an image");
                bat_command(string.Format("-i {0} -vf scale={1}:{2} {3}", input[0], width, height, temp));
                isimage = true;
            }
            else
            {
                Console.WriteLine("File is a video");
                temp = "Temp\\Temp2\\out_img%0d.jpg";
                bat_command(string.Format("-i \"{0}\" -r {4} -vf scale={1}:{2} {3}", input[0], width, height, temp, fps));
                bat_command(string.Format("-i \"{0}\" -f wav Temp\\out.wav", input[0]));
            }

            List<string> imgs = new List<string>();
            for (int i = 0; i < Directory.GetFiles("Temp2").Length; i++)
            {
                if (Directory.GetFiles("Temp\\Temp2")[i].Contains("out_img"))
                {
                    imgs.Add(Directory.GetFiles("Temp\\Temp2")[i]);
                }
            }

            //image = Image.FromFile(temp);

            //Gets the amount of frames within the image
            double frameCount = imgs.Count;

            int fontwidth = int.Parse(configs[2].Replace("font width (affects the scale of the characters themselves, which affects the image size) = ", ""));
            Font consolas = new Font("consolas", fontwidth * 2);

            //makes the character array
            Console.Clear();
            char[] chars = { ' ', ' ', '.', ',', ':', '=', '+', '*', '#', '%', '@' };
            //Array.Reverse(chars);
            Console.CursorVisible = false;
            //Starting the conversion from image to ASCII
            List<StringBuilder> ascii_ini = new List<StringBuilder>();


            StringBuilder[] ascii = new StringBuilder[int.Parse(frameCount.ToString())];




            //Image[] imgs = frames;


            for (int i = 0; i < frameCount; i++)
            {
                {
                    //selects the frame from the image represented by the "i"
                    Console.SetCursorPosition(0, 0);
                    StringBuilder sb = new StringBuilder();
                    //goes through each vertical row in the image
                    for (int h = 0; h < Image.FromFile(imgs[i]).Height; h++)
                    {
                        sb.Append("|");
                        //goes through each pixel in the row
                        for (int w = 0; w < Image.FromFile(imgs[i]).Width; w++)
                        {
                            //determines the brightnes of the pixel and converts it into the appropriate character from the chars array
                            Color cl = ((Bitmap)Image.FromFile(imgs[i])).GetPixel(w, h);
                            int gray = (cl.R + cl.G + cl.B) / 3;
                            int index = gray * (chars.Length - 1) / 255;

                            //writes the character into the stringbuilder
                            sb.Append(chars[index]);
                        }
                        sb.Append("|");
                        sb.Append('\n');
                    }
                    //ascii()(sb.ToString());
                    ascii[i] = sb;
                    //calculate the percentage and draws the progress to the progress bar
                    double percent = Math.Round((i / frameCount) * 100);
                    //Console.Clear();

                    Console.Write(sb.ToString());
                    Console.WriteLine("Frame {0}/{1}\n{2}%", i, frameCount, percent);
                    //File.WriteAllText("thing.txt", sb.ToString());
                    sb.Clear();

                    GC.Collect();
                }
            };



            Console.Clear();
            double i_d = 0;
            Parallel.For(0, ascii.Length, new ParallelOptions { MaxDegreeOfParallelism = 4 }, e =>
            {
                Console.SetCursorPosition(0, 0);

                //Draw the string to an image file
                DrawText(ascii[e].ToString(), consolas, Color.FromArgb(255, 255, 255), 30000, @"Temp\" + e + ".jpg");
                Console.SetCursorPosition(0, 0);
                i_d += 1;
                Console.WriteLine("\r" + i_d + "/" + frameCount);

                Console.SetCursorPosition(0, 0);
                GC.Collect();
            });
            image.Dispose();
            Console.Clear();
            //establishes the output resolution
            image = Image.FromFile(@"Temp\" + 0 + ".jpg");
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

            //If the original file is a video the program combines all the images into an mp4
            //otherwise the program assumes that the original file was an image and just copies the new image into the Output folder
            string filename = Path.GetFileNameWithoutExtension(input[0]);
            if (!isimage)
            {
                bat_command("-framerate " + fps + @" -i Temp\%%d.jpg -s " + width + "x" + height + " Temp\\outputs.mp4");

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
            Directory.Delete(folders[2], true);
            File.Delete("command.bat");
            Environment.Exit(1);

        }
    }
}
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace EmlAttachmentExtractor
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: EmlAttachmentExtractor [fileName]");
                return;
            }

            var prm = args[0];

            var path = Path.GetDirectoryName(prm);
            var file = prm.Remove(0, path.Length);

            if (path == "")
                path = ".";

            if (file == "")
                file = "*.eml";

            foreach (var fn in Directory.EnumerateFiles(path, file))
                ProcessFile(fn);
        }

        private static void ProcessFile(string fn)
        {
            Console.WriteLine(fn);
            var state = 0;
            var boundary = "";
            var imageData = new StringBuilder();
            //var imageType = "";
            var imageName = "";

            using (var data = File.OpenText(fn))
            {
                while (!data.EndOfStream)
                {
                    var line = data.ReadLine();

                    switch (state)
                    {
                        case 0:
                            {
                                var m = Regex.Match(line, @"boundary\=\""(.*?)\""");

                                if (m.Success)
                                {
                                    boundary = m.Groups[1].Value;
                                    state = 1;
                                }
                            }
                            break;

                        case 1:
                            {
                                if (line.IndexOf(boundary) >= 0)
                                {
                                    state = 2;
                                }
                            }
                            break;

                        case 2:
                            {
                                if (line.IndexOf(boundary) < 0)
                                {
                                    var m = Regex.Match(line, @"Content-Type\: image\/(\w+)");

                                    if (m.Success)
                                    {
                                        state = 3;
                                        //imageType = m.Groups[1].Value;
                                    }
                                }
                            }
                            break;

                        case 3:
                            {
                                if (line.IndexOf(boundary) >= 0)
                                {
                                    state = 2;
                                }
                                else
                                {
                                    var m = Regex.Match(line, @"Content-Disposition\: inline; filename=""(.*?)""");

                                    if (m.Success)
                                    {
                                        state = 4;
                                        imageName = m.Groups[1].Value;
                                        imageData.Clear();
                                    }
                                }

                            }
                            break;

                        case 4:
                            {
                                if (line.IndexOf(boundary) >= 0)
                                {
                                    state = 2;

                                    var bytes = Convert.FromBase64String(imageData.ToString());
                                    var path = Path.Combine(Path.GetDirectoryName(fn), imageName);

                                    File.WriteAllBytes(path, bytes);

                                    Console.WriteLine($"  {imageName} {bytes.Length} bytes.");
                                }
                                else
                                {
                                    if (!string.IsNullOrWhiteSpace(line))
                                        imageData.Append(line);
                                }
                            }
                            break;
                    }


                }
            }



        }
    }
}

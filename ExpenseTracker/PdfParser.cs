using System;
using System.Collections.Generic;
using System.Linq;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace ExpenseTracker
{
    public class PdfParser
    {
        public static List<List<string>> GetDocumentLines(string file)
        {
            List<List<string>> lines = new List<List<string>>();

            using (PdfDocument document = PdfDocument.Open(file))
            {
                foreach (Page page in document.GetPages())
                {
                    int lastY = -1;
                    List<string> currentLine = null;
                    foreach (Word word in page.GetWords())
                    {
                        if (word.TextOrientation != TextOrientation.Horizontal)
                        {
                            // Skip non-horizontal words
                            continue;
                        }

                        // Assume words that appear to have identical Y coordinates are one line
                        if (lastY == -1 || Math.Abs(lastY - word.BoundingBox.Bottom) > 5)
                        {
                            if (currentLine != null)
                            {
                                lines.Add(currentLine);
                            }

                            currentLine = new List<string>();
                            lastY = (int)word.BoundingBox.Bottom;
                        }

                        currentLine.Add(word.Text);
                    }

                    if (currentLine != null && currentLine.Any())
                    {
                        lines.Add(currentLine);
                    }
                }
            }

            return lines;
        }
    }
}

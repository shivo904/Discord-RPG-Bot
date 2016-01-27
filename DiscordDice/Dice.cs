using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordDice
{
    class Dice
    {
        /// <summary>Our Random object.  Make it a first-class citizen so that it produces truly *random* results</summary>
        Random r = new Random();

        /// <summary>Roll</summary>
        /// <param name="s">string to be evaluated</param>
        /// <returns>result of evaluated string</returns>
        public int R(string s)
        {
            int t = 0;

            // Addition is lowest order of precedence
            var a = s.Split('+');

            // Add results of each group
            if (a.Count() > 1)
                foreach (var b in a)
                    t += R(b);
            else
            {
                // Multiplication is next order of precedence
                var m = a[0].Split('*');

                // Multiply results of each group
                if (m.Count() > 1)
                {
                    t = 1; // So that we don't zero-out our results...

                    foreach (var n in m)
                        t *= R(n);
                }
                else
                {
                    // Die definition is our highest order of precedence
                    var d = m[0].Split('d');

                    // This operand will be our die count, static digits, or else something we don't understand
                    if (!int.TryParse(d[0].Trim(), out t))
                        t = 0;

                    int f;

                    // Multiple definitions ("2d6d8") iterate through left-to-right: (2d6)d8
                    for (int i = 1; i < d.Count(); i++)
                    {
                        // If we don't have a right side (face count), assume 6
                        if (!int.TryParse(d[i].Trim(), out f))
                            f = 6;

                        int u = 0;

                        // If we don't have a die count, use 1
                        for (int j = 0; j < (t == 0 ? 1 : t); j++)
                            u += r.Next(1, f);

                        t += u;
                    }
                }
            }

            return t;
        }
    }
}

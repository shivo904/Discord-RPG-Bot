﻿
using Discord;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiscordDice
{
    class Program
    {
        public static List<string> list = new List<string>();
        static DiscordClient client = new DiscordClient();
        static List<string> commandList = new List<string>() { "!repeat", "!status", "!roll", "!travel", "!resettravel", "!places", "!setbackground", "!background" };
        static void Main(string[] args)
        {
            

            //Display all log messages in the console
            client.LogMessage += (s, e) => Console.WriteLine($"[{e.Severity}] {e.Source}: {e.Message}");

            //Echo back any message received, provided it didn't come from the bot itself
            client.MessageReceived += async (s, e) =>
            {
                if (!e.Message.IsAuthor && e.Message.Channel.ToString() == "bot-testing")
                {
                    if (e.Message.RawText.ToLower().StartsWith("!repeat"))
                    {
                        await client.SendMessage(e.Channel, e.Message.Text);
                    }
                    else if (e.Message.RawText.ToLower().StartsWith("!status"))
                    {
                        await client.SendMessage(e.Channel, "I am up and running!");
                    }
                }
                else if(!e.Message.IsAuthor)
                {
                    if (e.Message.RawText.ToLower().StartsWith("!roll"))
                    {
                        string mes = RollDice(e.Message.RawText, e.Message.User.ToString());
                        int count = (mes.Length / 1999) + 1;
                        for (int i = 0; i < count; i++)
                        {
                            string test = mes.Substring(0, Math.Min(mes.Length, 1999));
                            await client.SendMessage(e.Channel, test);
                            mes = mes.Substring(Math.Min(1999, mes.Length - 1));
                        }
                    }
                    if (e.Message.RawText.ToLower().StartsWith("!travel"))
                    {
                        await client.SendMessage(e.Channel, ManageRole(e.User, e.Channel.Server, e.Channel, e.Message.RawText));
                    }
                    if (e.Message.RawText.ToLower().StartsWith("!places"))
                    {
                        await client.SendMessage(e.Channel, GetChannels(e.Server));
                    }
                    if (e.Message.RawText.ToLower().StartsWith("!setbackground"))
                    {
                        await client.SendMessage(e.Channel, SetBackground(e.Message));
                    }
                    if (e.Message.RawText.ToLower().StartsWith("!background"))
                    {
                        await client.SendMessage(e.Channel, Background(e.Channel));
                    }
                    if (e.Message.RawText.ToLower().StartsWith("!resettravel"))
                    {
                        await client.SendMessage(e.Channel, ResetRole(e.Channel.Server, e.Message.User));
                    }
                    if (e.Message.RawText.ToLower().StartsWith("!help") || e.Message.RawText.ToLower().StartsWith("!commands"))
                    {
                        await client.SendMessage(e.Channel, string.Join("\n",commandList.ToArray()));
                    }
                    if (e.Message.RawText.ToLower().StartsWith("!magic") || e.Message.RawText.ToLower().StartsWith("!cast"))
                    {
                        await client.SendMessage(e.Channel, UseMagic(e.Server, e.Message.User, e.Message.Channel));
                    }
                }
            };
            
            //Convert our sync method to an async one and block the Main function until the bot disconnects
            client.Run(async () =>
            {
                //Connect to the Discord server using our email and password
                await client.Connect(ConfigurationManager.AppSettings["email"], ConfigurationManager.AppSettings["pass"]);
            });
        }

        private static string UseMagic(Server server, User user, Channel channel)
        {
            foreach(User u in server.Members)
            {
                foreach(Role r in u.Roles)
                {
                    if(r.Name == "Game Master")
                    {
                        client.SendPrivateMessage(u, user.Name+" has used a magic ability in "+channel.Name+".");
                    }
                }
            }
            return "Sent";
        }

        #region Dice
        private static string RollDice(string message, string name)
        {
            list.Clear();
            string diceNotation = message.Split(new char[] { ' ' } , 2)[1].Replace(" ","");
            int total = 0;
            string[] seperateDice = diceNotation.Split('+');
            foreach (string s in seperateDice)
            {
                total += dice(s);
                list.Add("+");
            }

            string output = name + " rolls "+diceNotation+" :\n";
            for (int i = 0; i < list.Count-1; i++)
            {
                if (list[i] != "+" && list[i] != "*")
                    output += "[" + list[i] + "]";
                else
                    output += " " + list[i] + " ";
            }
            output += "\nTotal: **" + total+"**";
            return output;
        }

        public static int dice(string s)
        {
            Random rnd = new Random();
            Regex diceNotation = new Regex(@"^(\d+)(d|D)(\d+)");
            int output = 0;
            if (int.TryParse(s, out output))
            {
                list.Add(s);
                return output;
            }
            else if (diceNotation.IsMatch(s))
            {
                try {
                    string[] splitDice = s.ToLower().Split('d');
                    int numberOfDice = Convert.ToInt32(splitDice[0]);
                    int diceSides = Convert.ToInt32(splitDice[1]);
                    for (int i = 0; i < numberOfDice; i++)
                    {
                        int randomNumber = rnd.Next(diceSides) + 1;
                        list.Add(randomNumber.ToString());
                        output += randomNumber;
                    }
                }
                catch(Exception)
                {
                    return 0;
                }
            }
            return output;
        }
        #endregion

        #region Travel System
        private static string GetChannels(Server server)
        {
            string output = "";
            List<string> list = new List<string>();
            foreach (Channel c in server.Channels)
            {
                if (c.Name.ToLower() != "general")
                {
                    list.Add(c.Name);
                }
            }
            list.Sort();
            foreach(string s in list)
            {
                output += s + "\n";
            }
            return output;
        }

        private static string ManageRole(User user, Server server, Channel channel, string rawText)
        {
            bool channelExists = false;
            string travelChannel = rawText.Split(new char[] { ' ' }, 2)[1].ToLower();

            //Make sure this has nothing to do with the general channel
            if (travelChannel == "general")
            {
                return "You can't travel to the general chat.";
            }
            if (channel.Name.ToLower() == "general")
            {
                return "You can't travel from the general chat.";
            }

            foreach(Channel c in server.Channels)
            {
                if (c.Name == travelChannel)
                {
                    channelExists = true;
                }
            }
            if(!channelExists)
            {
                return "That place does not exist. Check your spelling.";
            }
            foreach (Channel c in server.Channels)
            {
                if (c.Name != travelChannel && c.Name.ToLower() != "general")
                {
                    DualChannelPermissions dual = new DualChannelPermissions();
                    dual.ReadMessages = false;
                    client.SetChannelPermissions(c, user, dual);
                }
                else if(c.Name.ToLower() == "general")
                {
                    
                }
                else
                {
                    DualChannelPermissions dual = new DualChannelPermissions();
                    dual.ReadMessages = true;
                    client.SetChannelPermissions(c, user, dual);
                    client.SendMessage(c, user.Name + " has arrived");
                }
            }
            return user.Name + " has left this place.";
        }
        private static string ResetRole(Server server, User user)
        {
            foreach(Channel c in server.Channels)
            {
                DualChannelPermissions dual = new DualChannelPermissions();
                dual.ReadMessages = null;
                client.SetChannelPermissions(c, user, dual);
            }
            return "Travel reset, please use any of the non-'general' places when you are ready to travel again.";
        }
        #endregion

        #region background
        private static string SetBackground(Message message)
        {
            if(File.Exists(message.Channel.Name))
            {
                File.Delete(message.Channel.Name);
            }
            string output = message.RawText.Split(new char[] { ' ' }, 2)[1];
            File.WriteAllText(message.Channel.Name, output);
            return "Background has been set for " + message.Channel.Name;
        }
        private static string Background(Channel channel)
        {
            string output;
            if (!File.Exists(channel.Name))
            {
                output = "Background has not been set for " + channel.Name;
            }
            else
            {
                output = File.ReadAllText(channel.Name);
            }
            return output;
        }
        #endregion
    }
}
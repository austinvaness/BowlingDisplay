using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        // =========== Settings ===========
        // The name of the panel to use as a display.
        // Leave as blank to select the first available display.
        readonly string panelName = "";

        // When true, the score <number> command will use the specified number as the
        // number of pins that are still standing.
        // When false, the score <number> command will use the specified number as the
        // number of pins that have been knocked down.
        readonly bool invertInput = false;
        // ================================

        // =========== Commands ===========
        // next : Selects the next player.
        // remove : Removes a score from the selected player.
        // clear : Removes all scores from the selected player.
        // score <number> : Adds a score to the selected player.
        //          The number is either the number of pins remaining, or
        //          the number of pins knocked down depending on the invertInput setting.
        // ================================



        string customData = "";
        Player [] players = new Player[0];
        int currPlayer = 0;
        Canvas canvas;
        
        public Program ()
        {
            if (panelName == "")
                canvas = new Canvas(GetBlock<IMyTextPanel>());
            else
                canvas = new Canvas(GetBlock<IMyTextPanel>(panelName));
            Parse();
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Parse()
        {
            List<Player> newPlayers = new List<Player>();
            string [] lines = Me.CustomData.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string l = lines [i].Trim();
                if (l.Length > 0)
                {
                    if (i < players.Length)
                    {
                        Player p = players [i];
                        p.Name = l;
                        newPlayers.Add(p);
                    }
                    else
                    {
                        newPlayers.Add(new Player(l));
                    }
                }

            }
            players = newPlayers.ToArray();
            if (currPlayer >= players.Length)
                currPlayer = 0;
            customData = Me.CustomData;
            Render();
        }

        public void Main (string argument, UpdateType updateSource)
        {
            if(updateSource == UpdateType.Update1 || updateSource == UpdateType.Update10 || updateSource == UpdateType.Update100)
            {
                if (Me.CustomData != customData)
                    Parse();
                Echo($"Running.\n{players.Length} players.");
                Render();
            }
            else
            {
                Command(argument);
            }
        }

        public void Command(string s)
        {
            string [] args = s.Trim().Split(' ');
            switch (args [0])
            {
                case "next":
                    currPlayer++;
                    if (currPlayer >= players.Length)
                        currPlayer = 0;
                    Render();
                    return;
                case "score":
                    if(players.Length > 0)
                    {
                        int n;
                        if (args.Length > 1 && int.TryParse(args [1], out n))
                        {
                            if (invertInput)
                                players [currPlayer].Bowl(n);
                            else
                                players [currPlayer].Bowl(10 - n);
                            Render();
                        }
                    }
                    return;
                case "remove":
                    if(players.Length > 0)
                    {
                        players [currPlayer].Remove();
                        Render();
                    }
                    return;
                case "clear":
                    if(players.Length > 0)
                    {
                        players [currPlayer].Clear();
                        Render();
                    }
                    return;
            }
        }

        private void Render()
        {
            for (int i = 0; i < players.Length; i++)
                players [i].Render(canvas, i, i == currPlayer);
            canvas.EndDraw();
        }

        private class Player
        {
            public string Name;
            private readonly List<int> balls = new List<int>();

            public Player(string name)
            {
                Name = name;
            }

            public void Bowl(int resultingPins)
            {
                balls.Add(MathHelper.Clamp(resultingPins, 0, 10));
            }

            public void Remove()
            {
                if (balls.Count > 0)
                    balls.RemoveAtFast(balls.Count - 1);
            }

            public void Clear()
            {
                balls.Clear();
            }

            public void Render(Canvas canvas, int index, bool highlight)
            {
                float fW = canvas.Size.X / 11;
                float fH = fW * (2 / 3f);
                Vector2 fSize = new Vector2(fW, fH);
                Vector2 fHalf = new Vector2(fW / 2, fH / 2);
                Vector2 subSize = new Vector2(fW / 3, fH / 2);
                float fontOffset;
                float fontSize = GetFontSize(canvas, subSize, out fontOffset);
                Vector2 subOffset1 = new Vector2(fW / 2, fH / 4);
                Vector2 subOffset1f = new Vector2(fW / 3 + fontOffset, 0);
                Vector2 subOffset2 = new Vector2(fW / 2 + fW / 3, fH / 4);
                Vector2 subOffset2f = new Vector2(fW * (2 / 3f) + fontOffset, 0);
                Vector2 subOffset3 = new Vector2(fW / 6, fH / 4);
                Vector2 subOffset3f = new Vector2(fontOffset, 0);
                Vector2 stringOffset = new Vector2(fontOffset, fW / 4 + 5);
                int ball = 0;
                int score = 0;
                for (int i = 0; i < 11; i++)
                {
                    Vector2 origin = new Vector2(i * fW, fH * index);
                    canvas.DrawSquare(origin + fHalf, fSize, Color.White);
                    string main = "";
                    if (i > 0)
                    {
                        canvas.DrawSquare(origin + subOffset1, subSize, Color.White);
                        canvas.DrawSquare(origin + subOffset2, subSize, Color.White);
                        if (ball < balls.Count)
                        {
                            int b1 = balls [ball];
                            ball++;
                            if (i < 10)
                            {
                                string s1 = "";
                                if (b1 == 0)
                                {
                                    s1 = "X";
                                    score += 10;
                                    if (ball < balls.Count)
                                    {
                                        int next = 10 - balls [ball];
                                        score += next;
                                        if (ball < balls.Count - 1)
                                        {
                                            if (next == 10)
                                                score += 10 - balls [ball + 1];
                                            else
                                                score += balls [ball] - balls [ball + 1];
                                        }
                                    }
                                    main = score.ToString();
                                }
                                else
                                {
                                    if (b1 == 10)
                                    {
                                        s1 = "-";
                                    }
                                    else
                                    {
                                        s1 = (10 - b1).ToString();
                                        score += 10 - b1;
                                    }

                                    if (ball < balls.Count)
                                    {
                                        int b2 = balls [ball];
                                        ball++;
                                        int diff = b1 - b2;
                                        string s2 = "";
                                        if (b2 == 0)
                                        {
                                            s2 = "/";
                                            if (ball < balls.Count)
                                                score += 10 - balls [ball];
                                        }
                                        else if (diff == 0)
                                        {
                                            s2 = "-";
                                        }
                                        else
                                        {
                                            s2 = diff.ToString();
                                        }
                                        score += diff;
                                        canvas.DrawString(origin + subOffset2f, s2, "monospace", Color.LightGray, fontSize);

                                    }
                                }
                                canvas.DrawString(origin + subOffset1f, s1, "monospace", Color.LightGray, fontSize);
                            }
                            else
                            {
                                string s1 = "";
                                string s2 = "";
                                string s3 = "";
                                if (b1 == 0)
                                {
                                    s1 = "X";
                                    score += 10;
                                    if (ball < balls.Count)
                                    {
                                        int b2 = balls [ball];
                                        ball++;
                                        if(b2 == 0)
                                        {
                                            s2 = "X";
                                            score += 10;
                                            b2 = 10;
                                        }
                                        else if(b2 < 10)
                                        {
                                            s2 = (10 - b2).ToString();
                                            score += 10 - b2;
                                        }
                                        else
                                        {
                                            s2 = "-";
                                        }

                                        if (ball < balls.Count)
                                        {
                                            int b3 = balls [ball];
                                            ball++;
                                            if (b3 == 0)
                                            {
                                                s3 = "X";
                                                score += 10;
                                            }
                                            else if (b3 < b2)
                                            {
                                                s3 = (b2 - b3).ToString();
                                                score += b2 - b3;
                                            }
                                            else
                                            {
                                                s3 = "-";
                                            }
                                        }
                                    }
                                }
                                else if(b1 < 10)
                                {
                                    s1 = (10 - b1).ToString();
                                    score += 10 - b1;
                                    if(ball < balls.Count)
                                    {
                                        int b2 = balls [ball];
                                        ball++;
                                        if(b2 == 0)
                                        {
                                            s2 = "/";
                                            score += b1 - b2;
                                            if (ball < balls.Count)
                                            {
                                                int b3 = balls [ball];
                                                ball++;
                                                if (b3 == 0)
                                                {
                                                    s3 = "X";
                                                    score += 10;
                                                }
                                                else if (b3 < 10)
                                                {
                                                    s3 = (10 - b3).ToString();
                                                    score += 10 - b3;
                                                }
                                                else
                                                {
                                                    s3 = "-";
                                                }
                                            }
                                        }
                                        else if (b2 < b1)
                                        {
                                            s2 = (b1 - b2).ToString();
                                            score += b1 - b2;
                                            // Game over
                                        }
                                        else
                                        {
                                            s2 = "-";
                                            // Game over
                                        }
                                    }
                                }
                                else
                                {
                                    s1 = "-";
                                    if (ball < balls.Count)
                                    {
                                        int b2 = balls [ball];
                                        ball++;
                                        if (b2 == 0)
                                        {
                                            s2 = "/";
                                            score += 10;
                                            if (ball < balls.Count)
                                            {
                                                int b3 = balls [ball];
                                                ball++;
                                                if (b3 == 0)
                                                {
                                                    s3 = "X";
                                                    score += 10;
                                                }
                                                else if (b3 < 10)
                                                {
                                                    s3 = (10 - b3).ToString();
                                                    score += 10 - b3;
                                                }
                                                else
                                                {
                                                    s3 = "-";
                                                }
                                            }
                                        }
                                        else if (b2 < 10)
                                        {
                                            s2 = (10 - b2).ToString();
                                            score += 10 - b2;
                                            // Game over
                                        }
                                        else
                                        {
                                            s2 = "-";
                                            // Game over
                                        }
                                    }
                                }
                                canvas.DrawString(origin + subOffset3f, s1, "monospace", Color.LightGray, fontSize);
                                canvas.DrawString(origin + subOffset1f, s2, "monospace", Color.LightGray, fontSize);
                                canvas.DrawString(origin + subOffset2f, s3, "monospace", Color.LightGray, fontSize);
                            }
                            main = score.ToString();
                        }
                        if(i == 10)
                            canvas.DrawSquare(origin + subOffset3, subSize, Color.White);
                    }
                    else
                    {
                        if (highlight)
                            main = ">" + Name + "<";
                        else
                            main = Name;
                    }
                    if (main.Length > 5)
                        main = main.Substring(0, 5);
                    if(main.Length > 0)
                        canvas.DrawString(origin + stringOffset, main, "monospace", Color.LightGray, fontSize);
                }
            }

            private float GetFontSize(Canvas canvas, Vector2 size, out float padding)
            {
                Vector2 charSize = canvas.GetStringSize("X", "Monospace", 1);
                Vector2 result = new Vector2(size.X / charSize.X, size.Y / charSize.Y);
                if(result.Y < result.X)
                {
                    padding = (size.X - charSize.X) * result.Y * 0.5f + 1;
                    return result.Y;
                }
                padding = 0;
                return result.X;
            }
        }
    }
}

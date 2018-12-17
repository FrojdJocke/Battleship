using SinkMyBattleship_2._0.Models;
using SinkMyBattleship_2._0.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SinkMyBattleship_2._0.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _action;
        public Player Opponent { get; set; }
        
        public MainViewModel(Player player)
        {
            if (string.IsNullOrEmpty(player.Address))
            {
                Task.Run(() => StartServer(player));
            }
            else
            {
                Task.Run(() => StartClient(player));
            }
            Opponent = new Player(null,null,0,new List<Boat>
            {
                new Boat("Carrier", new Dictionary<string, bool>(){ {"A1",false },{"A2",false },{"A3",false },{"A4",false },{"A5",false } }),
                new Boat("Battleship", new Dictionary<string, bool>() { { "B1", false },{ "B2", false }, { "B3", false }, { "B4", false} }),
                new Boat("Destroyer", new Dictionary<string, bool>() { { "C1", false },{ "C2", false }, { "C3", false } }),
                new Boat("Submarine",new Dictionary<string, bool>() { { "D1", false },{ "D2", false }, { "D3", false } }),
                new Boat("Patrol Boat", new Dictionary<string, bool>() { { "E1", false },{ "E2", false }})
            });
            
        }

        public static Logger Logger { get; set; } = new Logger();

        public event PropertyChangedEventHandler PropertyChanged;

        static TcpListener listener;

        public string LastAction { get; set; }

        public string Action
        {
            get => _action;
            set
            {
                _action = value;
                OnPropertyChanged(nameof(Action));
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        private async void StartClient(Player player)
        {
            using (var client = new TcpClient(player.Address, player.Port))
            using (var networkStream = client.GetStream())
            using (var reader = new StreamReader(networkStream, Encoding.UTF8))
            using (var writer = new StreamWriter(networkStream, Encoding.UTF8) { AutoFlush = true })
            {
                LastAction = "";
                bool continuePlay = true;
                var command = reader.ReadLine();
                if (command != null && command.StartsWith("210"))
                {
                    Logger.AddToLog($"Ansluten till {client.Client.RemoteEndPoint}");
                    writer.WriteLine($"HELO {player.Name}");
                    Logger.AddToLog($"HELO {player.Name}");
                }
                else{continuePlay = false;}

                command = reader.ReadLine();
                if (command != null && command.StartsWith("220"))
                {
                    writer.WriteLine("START");
                    Logger.AddToLog("START");
                }
                else {continuePlay = false;}
                command = reader.ReadLine();
                if (command == null || !command.StartsWith("22") || command.StartsWith("220")) continuePlay = false;


                if (continuePlay)
                {
                    if(command.Split(' ')[0] == "221")
                    {
                        player.PlayTurn = 1;
                        Opponent.PlayTurn = 2;
                    }
                    else
                    {
                        player.PlayTurn = 2;
                        Opponent.PlayTurn = 1;
                    }
                }
                while (client.Connected && continuePlay)
                {
                    for (int i = 1; i < 3; i++)
                    {
                        if(player.PlayTurn == i)
                        {
                            //Client logik
                            while (true)
                            {
                                if (!string.IsNullOrWhiteSpace(LastAction))
                                {
                                    if (LastAction.ToUpper() == "QUIT")
                                    {
                                        Logger.AddToLog(StatusCode.ConnectionLost.GetDescription());
                                        writer.WriteLine(StatusCode.ConnectionLost.GetDescription());
                                        continuePlay = false;
                                        break;
                                    }
                                    if (LastAction.StartsWith("FIRE ", StringComparison.InvariantCultureIgnoreCase) && FireSyntaxCheck(LastAction))
                                    {
                                        writer.WriteLine(LastAction);
                                        //Spel logik
                                        var response = reader.ReadLine();
                                        if (response == null || response.StartsWith("270"))
                                        {
                                            Logger.AddToLog(StatusCode.ConnectionLost.GetDescription());
                                            continuePlay = false;
                                            break;
                                        }
                                        if (response.StartsWith("5"))
                                        {
                                            Logger.AddToLog(response);
                                            LastAction = "";
                                            continue;
                                        }
                                        if (response.StartsWith("260"))
                                        {
                                            Logger.AddToLog(StatusCode.YouWin.GetDescription());
                                            continuePlay = false;
                                            break;
                                        }
                                        Logger.AddToLog(response);

                                        LastAction = "";
                                        break;
                                    }                                    
                                }
                                else
                                {
                                    Thread.Sleep(500);
                                }
                            }
                            Logger.AddToLog("Waiting for opponents action..");
                        }
                        if(Opponent.PlayTurn == i)
                        {
                            //Host logik
                            while (true)
                            {
                                command = reader.ReadLine();
                                if (command == null || command.StartsWith("270"))
                                {
                                    Logger.AddToLog(StatusCode.ConnectionLost.GetDescription());
                                    continuePlay = false;
                                    break;
                                }
                                if(command.StartsWith("FIRE", StringComparison.InvariantCultureIgnoreCase) && FireSyntaxCheck(command))
                                {
                                    Logger.AddToLog($"Opponent: {command}");

                                    var fireResponse = player.GetFiredAtMessage(command);

                                    if (fireResponse.StartsWith("260"))
                                    {
                                        Logger.AddToLog("You Lost!");
                                        writer.WriteLine(fireResponse);
                                        continuePlay = false;
                                        break;
                                    }

                                    if (fireResponse.StartsWith("230"))
                                    {
                                        Logger.AddToLog($"Opponent: {fireResponse}");
                                        writer.WriteLine(fireResponse);
                                        break;
                                    }
                                    Logger.AddToLog($"Opponent: {fireResponse}");
                                    writer.WriteLine(fireResponse);

                                    break;
                                }
                                else
                                {
                                    var syntax = SyntaxCheck(command);
                                    StatusWriter(writer, syntax ? StatusCode.SequenceError : StatusCode.SyntaxError);
                                }
                            }
                            
                        }
                        if (!continuePlay) break;
                    }

                    

                    


                };

            }
        }

        static void StartListen(int port)
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                Logger.AddToLog($"Starts listening on port: {port}");
            }
            catch (SocketException)
            {
                Logger.AddToLog("Misslyckades att öppna socket. Troligtvis upptagen.");
                Environment.Exit(1);
            }
        }

        private async Task StartServer(Player player)
        {
            Logger.AddToLog("Välkommen till servern");

            StartListen(player.Port);

            while (true)
            {
                Logger.AddToLog("Väntar på att någon ska ansluta sig...");
                
                using (var client = await listener.AcceptTcpClientAsync())
                using (var networkStream = client.GetStream())
                using (var reader = new StreamReader(networkStream, Encoding.UTF8))
                using (var writer = new StreamWriter(networkStream, Encoding.UTF8) { AutoFlush = true })
                {
                    Logger.AddToLog($"Klient har anslutit sig {client.Client.RemoteEndPoint}!");                   
                    StatusWriter(writer, StatusCode.Battleship);
                    Logger.AddToLog("210 BATTLESHIP/1.0");

                    int errorCounter = 0;
                    var handshake = false;
                    var start = false;
                    bool continuePlay = true;
                    while (client.Connected && continuePlay)
                    {
                        
                        var command = "";
                        #region // Handshake
                        if (!handshake && continuePlay)
                        {
                            command = reader.ReadLine();
                            if(command.Equals("QUIT", StringComparison.InvariantCultureIgnoreCase))
                            {
                                break;
                            }
                            //clientName = reader.ReadLine();
                            if (command.StartsWith("helo ", StringComparison.InvariantCultureIgnoreCase))
                            {
                                handshake = true;
                                Logger.AddToLog(command);
                                writer.WriteLine($"220 {player.Name}");
                                Logger.AddToLog($"220 {player.Name}");
                                Opponent.Name = command.Split(' ')[1];
                            }
                            else
                            {
                                var syntax = SyntaxCheck(command);
                                if (syntax)
                                {
                                    StatusWriter(writer, StatusCode.SequenceError);
                                }
                                else
                                {
                                    StatusWriter(writer, StatusCode.SyntaxError);
                                }
                                errorCounter++;
                                if (errorCounter == 3)
                                {
                                    continuePlay = false;
                                    StatusWriter(writer, StatusCode.ConnectionLost);
                                    Logger.AddToLog(StatusCode.ConnectionLost.GetDescription());
                                    break;
                                }
                                continue;
                            }
                        }
                        #endregion
                        #region // Start
                        if(!start && handshake)
                        {
                            errorCounter = 0;
                            command = reader.ReadLine();
                            if (command.Equals("QUIT", StringComparison.InvariantCultureIgnoreCase))
                            {
                                break;
                            }
                            if (command.Equals("START", StringComparison.InvariantCultureIgnoreCase))
                            {
                                start = true;
                            }
                            else
                            {
                                var syntax = SyntaxCheck(command);
                                if (syntax)
                                {
                                    StatusWriter(writer, StatusCode.SequenceError);
                                }
                                else
                                {
                                    StatusWriter(writer, StatusCode.SyntaxError);
                                }

                                errorCounter++;
                                if (errorCounter == 3)
                                {
                                    continuePlay = false;
                                    StatusWriter(writer, StatusCode.ConnectionLost);
                                    Logger.AddToLog(StatusCode.ConnectionLost.GetDescription());
                                    break;
                                }
                                continue;
                                
                            }
                        }
                        #endregion

                        //Set starter
                        var random = new Random();
                        var number = random.Next(0, 2) + 1;
                        player.PlayTurn = number;
                        Opponent.PlayTurn = number == 1 ? 2 : 1;

                        if(player.PlayTurn == 1)
                        {
                            StatusWriter(writer, StatusCode.HostStart);
                            Logger.AddToLog(StatusCode.HostStart.GetDescription());
                        }
                        else
                        {
                            StatusWriter(writer, StatusCode.ClientStart);
                            Logger.AddToLog(StatusCode.ClientStart.GetDescription());
                        }                          

                        while (start && continuePlay)
                        {
                            for (int i = 1; i < 3; i++)
                            {
                                errorCounter = 0;
                                if(player.PlayTurn == i)
                                {
                                    // Wait for correct action from server
                                    while (true)
                                    {
                                        if (!string.IsNullOrEmpty(LastAction))
                                        {
                                            if(LastAction.ToUpper() == "QUIT")
                                            {
                                                StatusWriter(writer,StatusCode.ConnectionLost);
                                                continuePlay = false;
                                                break;
                                            }
                                            if (LastAction.StartsWith("270"))
                                            {
                                                StatusWriter(writer, StatusCode.ConnectionLost);
                                                continuePlay = false;
                                                break;
                                            }
                                            if (LastAction.StartsWith("FIRE ",StringComparison.InvariantCultureIgnoreCase) && FireSyntaxCheck(LastAction))
                                            {
                                                #region // Check if previously fired at
                                                if (player.CheckFiredAt(LastAction))
                                                {
                                                    Logger.AddToLog(StatusCode.SequenceError.GetDescription());
                                                    LastAction = "";
                                                    continue;
                                                }
                                                else
                                                {
                                                    player.PrevCoors.Add(LastAction.Split(' ')[1].ToUpper());
                                                }
                                                #endregion

                                                writer.WriteLine(LastAction); //Send Fire
                                                Logger.AddToLog($"You: {LastAction}");
                                                ///////Spel logik//////////////////////////////////////////////
                                                bool hit = false;
                                                while (true)
                                                {
                                                var response = reader.ReadLine();
                                                if (response == null)
                                                {
                                                    Logger.AddToLog(StatusCode.ConnectionLost.GetDescription());
                                                    continuePlay = false;
                                                    break;
                                                }

                                                if (!string.IsNullOrWhiteSpace(response))
                                                {
                                                    
                                                        if (response.StartsWith("QUIT", StringComparison.InvariantCultureIgnoreCase) || 
                                                            response.StartsWith("270"))
                                                        {
                                                            continuePlay = false;
                                                            Logger.AddToLog(StatusCode.ConnectionLost.GetDescription());
                                                            break;
                                                        }
                                                        if (response.StartsWith("260"))
                                                        {
                                                            Logger.AddToLog(StatusCode.YouWin.GetDescription());
                                                            continuePlay = false;
                                                            break;
                                                        }
                                                        if (response.StartsWith("230"))
                                                        {
                                                            errorCounter = 0;
                                                            hit = false;
                                                            break;
                                                        }
                                                        if (response.StartsWith("24") || response.StartsWith("25"))
                                                        {
                                                            Logger.AddToLog(response);
                                                            errorCounter = 0;
                                                            hit = true;
                                                            break;
                                                        }
                                                        else
                                                        {
                                                            var syntax = SyntaxCheck(response);
                                                            StatusWriter(writer, syntax ? StatusCode.SequenceError : StatusCode.SyntaxError);
                                                            errorCounter++;
                                                            if (errorCounter == 3)
                                                            {
                                                                continuePlay = false;
                                                                StatusWriter(writer, StatusCode.ConnectionLost);
                                                                Logger.AddToLog(StatusCode.ConnectionLost.GetDescription());
                                                                break;
                                                            }
                                                            continue;
                                                        }
                                                    }

                                                }
                                                if (!continuePlay) break;
                                                if (!hit)
                                                {
                                                    //writer.WriteLine(StatusCode.Miss.GetDescription());
                                                    Logger.AddToLog(StatusCode.Miss.GetDescription());
                                                }
                                                ///////////////////////////////////////////////////////////////
                                                Logger.AddToLog("Waiting for opponents action..");
                                                LastAction = "";
                                                break;
                                            }
                                            else
                                            {
                                                var syntax = SyntaxCheck(LastAction);
                                                if (!FireSyntaxCheck(LastAction))
                                                {
                                                    Logger.AddToLog(StatusCode.SyntaxError.GetDescription());
                                                }
                                                else if (syntax)
                                                {
                                                    Logger.AddToLog(StatusCode.SequenceError.GetDescription());
                                                }
                                                else
                                                {
                                                    Logger.AddToLog(StatusCode.SyntaxError.GetDescription());
                                                }

                                                errorCounter++;
                                                if (errorCounter == 3)
                                                {
                                                    continuePlay = false;
                                                    StatusWriter(writer, StatusCode.ConnectionLost);
                                                    Logger.AddToLog(StatusCode.ConnectionLost.GetDescription());
                                                    break;
                                                }
                                                LastAction = "";
                                            }
                                        }
                                        else
                                        {
                                            Thread.Sleep(500);
                                        }
                                    }
                                }
                                if (Opponent.PlayTurn == i)
                                {
                                    // wait for correct action from client
                                    while (true)
                                    {
                                        command = reader.ReadLine();
                                        if (command == null)
                                        {
                                            Logger.AddToLog("Client Lost");
                                            continuePlay = false;
                                            break;

                                        }
                                        if (!string.IsNullOrEmpty(command))
                                        {
                                            if (command.ToUpper() == "QUIT")
                                            {
                                                writer.WriteLine(StatusCode.ConnectionLost.GetDescription());
                                                continuePlay = false;
                                                break;
                                            }
                                            if (command.StartsWith("270"))
                                            {
                                                continuePlay = false;
                                                break;
                                            }
                                            if (command.StartsWith("FIRE ", StringComparison.InvariantCultureIgnoreCase) && FireSyntaxCheck(command))
                                            {
                                                #region // Check if previously fired at
                                                if (Opponent.CheckFiredAt(command))
                                                {
                                                    //Logger.AddToLog($"Client: {StatusCode.SequenceError.GetDescription()}");
                                                    writer.WriteLine(StatusCode.SequenceError.GetDescription());
                                                    errorCounter++;
                                                    if (errorCounter == 3)
                                                    {
                                                        continuePlay = false;
                                                        StatusWriter(writer, StatusCode.ConnectionLost);
                                                        Logger.AddToLog(StatusCode.ConnectionLost.GetDescription());
                                                        break;
                                                    }
                                                    continue;
                                                }
                                                else
                                                {
                                                    Opponent.PrevCoors.Add(command.Split(' ')[1].ToUpper());
                                                }
                                                #endregion
                                                
                                                Logger.AddToLog($"Client: {command}");
                                                ///////Spel logik//////////////////////////////////////////////
                                                bool hit = false;
                                                int boatEnum = 0;
                                                bool sunk = false;
                                                #region // Check for hit <OLD>
                                                //foreach (var boat in player.Boats)
                                                //{
                                                //    foreach (var coor in boat.Coordinates)
                                                //    {
                                                //        if (coor.Key == command.Split(' ')[1].ToUpper())
                                                //        {
                                                //            sunk = true;
                                                //            boat.Coordinates[coor.Key] = true;
                                                //            foreach (var c in boat.Coordinates)
                                                //            {
                                                //                if (c.Value == false)
                                                //                {
                                                //                    sunk = false;
                                                //                }
                                                //            }
                                                //            if (sunk)
                                                //            {
                                                //                boat.Alive = false;
                                                //                if (player.Boats.All(x => x.Alive == false))
                                                //                {
                                                //                    Logger.AddToLog($"Client: {StatusCode.YouWin.GetDescription()}");
                                                //                    writer.WriteLine(StatusCode.ConnectionLost.GetDescription());
                                                //                }
                                                //                switch (boat.Name)
                                                //                {
                                                //                    case "Carrier":
                                                //                        boatEnum = 251;
                                                //                        break;
                                                //                    case "Battleship":
                                                //                        boatEnum = 252;
                                                //                        break;
                                                //                    case "Destroyer":
                                                //                        boatEnum = 253;
                                                //                        break;
                                                //                    case "Submarine":
                                                //                        boatEnum = 254;
                                                //                        break;
                                                //                    case "Patrol Boat":
                                                //                        boatEnum = 255;
                                                //                        break;
                                                //                }
                                                //            }
                                                //            else
                                                //            {
                                                //                switch (boat.Name)
                                                //                {
                                                //                    case "Carrier":
                                                //                        boatEnum = 241;
                                                //                        break;
                                                //                    case "Battleship":
                                                //                        boatEnum = 242;
                                                //                        break;
                                                //                    case "Destroyer":
                                                //                        boatEnum = 243;
                                                //                        break;
                                                //                    case "Submarine":
                                                //                        boatEnum = 244;
                                                //                        break;
                                                //                    case "Patrol Boat":
                                                //                        boatEnum = 245;
                                                //                        break;
                                                //                }
                                                //            }
                                                //            //Hit

                                                //            var myEnum = (StatusCode)boatEnum;
                                                //            writer.WriteLine(myEnum.GetDescription());
                                                //            Logger.AddToLog(myEnum.GetDescription());
                                                //            hit = true;
                                                //            break;
                                                //        }
                                                //    }
                                                //    if (hit || sunk) break;
                                                //}
                                                #endregion

                                                var fireResponse = player.GetFiredAtMessage(command);
                                                if (fireResponse.StartsWith("260"))
                                                {
                                                    writer.WriteLine(fireResponse);
                                                    Logger.AddToLog("You Lost");
                                                    continuePlay = false;
                                                    break;
                                                }

                                                if (!fireResponse.StartsWith("230"))
                                                {
                                                    hit = true;
                                                    writer.WriteLine(fireResponse);
                                                    Logger.AddToLog($"Client: {fireResponse}");
                                                }

                                                if (!hit)
                                                {
                                                    writer.WriteLine(StatusCode.Miss.GetDescription());
                                                    Logger.AddToLog($"Client: {StatusCode.Miss.GetDescription()}");
                                                }
                                                ///////////////////////////////////////////////////////////////
                                                Logger.AddToLog("Your turn...");
                                                
                                                break;
                                            }
                                            else
                                            {
                                                var syntax = SyntaxCheck(command);
                                                if (!FireSyntaxCheck(command))
                                                {
                                                    StatusWriter(writer, StatusCode.SyntaxError);
                                                }
                                                else if (syntax)
                                                {
                                                    StatusWriter(writer, StatusCode.SequenceError);
                                                }
                                                else
                                                {
                                                    StatusWriter(writer, StatusCode.SyntaxError);
                                                }

                                                errorCounter++;
                                                if (errorCounter == 3)
                                                {
                                                    continuePlay = false;
                                                    StatusWriter(writer, StatusCode.ConnectionLost);
                                                    Logger.AddToLog(StatusCode.ConnectionLost.GetDescription());
                                                    break;
                                                }
                                                
                                            }
                                        }                                        


                                        //if (command.Equals("QUIT", StringComparison.InvariantCultureIgnoreCase))
                                        //{
                                        //    continuePlay = false;
                                        //    break;
                                        //}                                            
                                        //if (!string.IsNullOrEmpty(command))
                                        //{
                                        //    if (command.StartsWith("FIRE ",StringComparison.InvariantCultureIgnoreCase) && FireSyntaxCheck(command))
                                        //    {
                                        //        //Spel logik händer här

                                        //        Logger.AddToLog($"Klient: {command}");
                                        //        writer.WriteLine("2");
                                        //        LastAction = "";
                                        //        break;
                                        //    }
                                        //    else
                                        //    {
                                        //        var validSyntax = SyntaxCheck(command);
                                        //        if (!FireSyntaxCheck(LastAction))
                                        //        {
                                        //            StatusWriter(writer, StatusCode.SyntaxError);
                                        //        }
                                        //        else if (validSyntax)
                                        //        {
                                        //            StatusWriter(writer, StatusCode.SequenceError);
                                        //        }
                                        //        else
                                        //        {
                                        //            StatusWriter(writer, StatusCode.SyntaxError);
                                        //        }
                                        //    }

                                        //}
                                    }
                                }
                                if (!continuePlay)
                                    break;
                            }
                        }                        

                    }

                }


            }
        }


        public void SendAction()
        {
            LastAction = Action;
        }
        private bool FireSyntaxCheck(string input)
        {
            input = input.Split(' ')[1].ToUpper();

            if (input.Length < 1 || input.Length > 3)
                return false;
            var character = input[0];
            if (character < 'A' || character > 'J')
            {
                return false;
            }
            int num;
            int num2;
            var isNum = int.TryParse(input[1].ToString(), out num);
            if (!isNum)
            {
                return false;
            }
            if (input.Length == 3)
            {
                isNum = int.TryParse(input[2].ToString(), out num2);
                if (!isNum)
                    return false;
                var textNum = num.ToString() + num2.ToString();
                num = int.Parse(textNum);
            }
            if (num < 1 || num > 10)
            {
                return false;
            }
            return true;
        }

        private bool SyntaxCheck(string input)
        {
            input = input.Split(' ')[0].ToUpper();
            var commands = new List<string>() { "HELO", "START", "FIRE","HELP" };

            if (!commands.Contains(input))
                return false;
            return true;
        }
        private void StatusWriter(StreamWriter writer, StatusCode code)
        {
            writer.WriteLine(code.GetDescription());
        }
        
    }
}

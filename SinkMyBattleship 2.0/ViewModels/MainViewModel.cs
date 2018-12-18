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
using System.Windows;
using Caliburn.Micro;

namespace SinkMyBattleship_2._0.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _action;
        public Player Opponent { get; set; }
        public Player Player { get; set; }
        public Boat Boat1 { get; set; }
        public Boat Boat2 { get; set; }
        public Boat Boat3 { get; set; }
        public Boat Boat4 { get; set; }
        public Boat Boat5 { get; set; }
        public MainViewModel(Player player)
        {
            Player = player;

            Opponent = new Player(null, null, 0, new List<Boat>
            {
                //new Boat("Carrier", new Dictionary<string, bool>(){ {"A1",false },{"A2",false },{"A3",false },{"A4",false },{"A5",false } }),
                //new Boat("Battleship", new Dictionary<string, bool>() { { "B1", false },{ "B2", false }, { "B3", false }, { "B4", false} }),
                //new Boat("Destroyer", new Dictionary<string, bool>() { { "C1", false },{ "C2", false }, { "C3", false } }),
                //new Boat("Submarine",new Dictionary<string, bool>() { { "D1", false },{ "D2", false }, { "D3", false } }),
                //new Boat("Patrol Boat", new Dictionary<string, bool>() { { "E1", false },{ "E2", false }})
            });

            if(Player.Boats != null)
            {
                Boat1 = Player.Boats[0];
                Boat2 = Player.Boats[1];
                Boat3 = Player.Boats[2];
                Boat4 = Player.Boats[3];
                Boat5 = Player.Boats[4];
                foreach (var item in Player.Boats)
                {
                    item.Position.Column += 11;
                }

            }
            try
            {
                if (string.IsNullOrEmpty(player.Address))
                {
                    Task.Run(() => StartServer(player));
                }
                else
                {
                    Task.Run(() => StartClient(player));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                listener?.Stop();
                var manager = new WindowManager();
                manager.ShowWindow(new ShellViewModel());
                Application.Current.Windows[0].Close();
            }
        }

        public static Logger Logger { get; set; } = new Logger();

        public event PropertyChangedEventHandler PropertyChanged;

        static TcpListener listener;

        static TcpClient clientTcp;

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
                Logger.AddToLog("Press Restart");
            }
            catch (IOException e)
            {
                Logger.AddToLog("Something went wrong. Press Restart");
            }
        }

        private async void StartClient(Player player)
        {
            try
            {
                //clientTcp = new TcpClient(player.Address, player.Port);
                using (var client = new TcpClient(player.Address, player.Port))
                //using(clientTcp)//
                using (var networkStream = client.GetStream())
                //using (var networkStream = clientTcp.GetStream())//
                using (var reader = new StreamReader(networkStream, Encoding.UTF8))
                using (var writer = new StreamWriter(networkStream, Encoding.UTF8) {AutoFlush = true})
                {
                    //clientTcp = client;
                    LastAction = "";
                    bool continuePlay = true;
                    var command = reader.ReadLine();
                    if (command != null && command.Equals("210 BATTLESHIP/1.0"))
                    {
                        Logger.AddToLog($"Ansluten till {clientTcp.Client.RemoteEndPoint}");
                        writer.WriteLine($"HELO {player.Name}");
                        Logger.AddToLog($"HELO {player.Name}");
                    }
                    else
                    {
                        Logger.AddToLog("Something went wrong. Aborted connection");
                        continuePlay = false;
                    }

                    command = reader.ReadLine();
                    if (command != null && command.StartsWith("220"))
                    {
                        Logger.AddToLog(command);
                        writer.WriteLine("START");
                        Logger.AddToLog("START");
                    }
                    else
                    {
                        continuePlay = false;
                    }

                    command = reader.ReadLine();
                    if (command == null || !command.StartsWith("22") || command.StartsWith("220")) continuePlay = false;
                    Logger.AddToLog(command);

                    if (continuePlay)
                    {
                        if (command.Split(' ')[0] == "221")
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

                    while (clientTcp.Connected && continuePlay)
                    {
                        for (int i = 1; i < 3; i++)
                        {
                            if (player.PlayTurn == i)
                            {
                                Logger.AddToLog("Your Turn");
                                //Client logik
                                while (true)
                                {
                                    if (!string.IsNullOrWhiteSpace(LastAction))
                                    {
                                        if (LastAction.ToUpper() == "QUIT")
                                        {
                                            Logger.AddToLog(StatusCode.ConnectionLost.GetDescription());
                                            writer.WriteLine(LastAction);
                                            continuePlay = false;
                                            break;
                                        }

                                        if (LastAction.StartsWith("FIRE ",
                                                StringComparison.InvariantCultureIgnoreCase) &&
                                            FireSyntaxCheck(LastAction))
                                        {
                                            writer.WriteLine(LastAction);
                                            Opponent.Command = $"{LastAction} ";
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
                                            Opponent.Command += response;
                                            Opponent.GetFiredAtForUI();
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

                            if (Opponent.PlayTurn == i)
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

                                    if (command.StartsWith("FIRE", StringComparison.InvariantCultureIgnoreCase) &&
                                        FireSyntaxCheck(command))
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
                                        StatusWriter(writer,
                                            syntax ? StatusCode.SequenceError : StatusCode.SyntaxError);
                                    }
                                }
                            }

                            if (!continuePlay) break;
                        }
                    }
                }
            }
            catch (SocketException e)
            {
                Logger.AddToLog(e.Message);
                Logger.AddToLog("Press Restart");
            }
            catch (System.IO.IOException e)
            {
                Logger.AddToLog(StatusCode.ConnectionLost.GetDescription());
                Logger.AddToLog("Press restart");
                
            }
        }

        private async Task StartServer(Player player)
        {
            LastAction = "";
            StartListen(player.Port);

            while (true)
            {
                //Clear History
                foreach (var boat in player.Boats)
                {
                    boat.Coordinates = boat.Coordinates.ToDictionary(x => x.Key, x => false);
                }
                player.PrevCoors = new List<string>();
                Player.ClearBoard();
                Opponent.ClearBoard();
                LastAction = "";
                Logger.AddToLog("Väntar på att någon ska ansluta sig...");
                try
                {
                    using (var client = await listener.AcceptTcpClientAsync())
                    using (var networkStream = client.GetStream())
                    using (var reader = new StreamReader(networkStream, Encoding.UTF8))
                    using (var writer = new StreamWriter(networkStream, Encoding.UTF8) {AutoFlush = true})
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
                                if (command == null ||
                                    command.Equals("QUIT", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    break;
                                }

                                //clientName = reader.ReadLine();
                                if (command.StartsWith("helo ", StringComparison.InvariantCultureIgnoreCase) ||
                                    command.StartsWith("hello ", StringComparison.InvariantCultureIgnoreCase))
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
                                    if (errorCounter > 3)
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

                            if (!start && handshake)
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
                                    if (errorCounter > 3)
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

                            if (player.PlayTurn == 1)
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
                                    if (player.PlayTurn == i)
                                    {
                                        // Wait for correct action from server
                                        while (true)
                                        {
                                            if (!string.IsNullOrEmpty(LastAction))
                                            {
                                                if (LastAction.ToUpper() == "QUIT")
                                                {
                                                    StatusWriter(writer, StatusCode.ConnectionLost);
                                                    continuePlay = false;
                                                    break;
                                                }

                                                if (LastAction.StartsWith("270"))
                                                {
                                                    StatusWriter(writer, StatusCode.ConnectionLost);
                                                    continuePlay = false;
                                                    break;
                                                }

                                                if (LastAction.StartsWith("FIRE ",
                                                        StringComparison.InvariantCultureIgnoreCase) &&
                                                    FireSyntaxCheck(LastAction))
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
                                                    Opponent.Command = $"{LastAction} ";
                                                    Logger.AddToLog($"You: {LastAction}");
                                                    ///////Spel logik//////////////////////////////////////////////
                                                    //bool hit = false;
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
                                                            if (response.StartsWith("QUIT",
                                                                    StringComparison.InvariantCultureIgnoreCase) ||
                                                                response.StartsWith("270"))
                                                            {
                                                                continuePlay = false;
                                                                Logger.AddToLog(
                                                                    StatusCode.ConnectionLost.GetDescription());
                                                                break;
                                                            }

                                                            if (response.StartsWith("260"))
                                                            {
                                                                Logger.AddToLog(StatusCode.YouWin.GetDescription());
                                                                continuePlay = false;
                                                                break;
                                                            }

                                                            //if (response.StartsWith("230"))
                                                            //{
                                                            //    Logger.AddToLog(response);
                                                            //    errorCounter = 0;
                                                            //    hit = false;
                                                            //    break;
                                                            //}

                                                            if (response.StartsWith("230") || response.StartsWith("24") || response.StartsWith("25"))
                                                            {
                                                                Opponent.Command += response;
                                                                Opponent.GetFiredAtForUI();
                                                                Logger.AddToLog(response);
                                                                errorCounter = 0;
                                                                //hit = true;
                                                                break;
                                                            }
                                                            else
                                                            {
                                                                var syntax = SyntaxCheck(response);
                                                                StatusWriter(writer,
                                                                    syntax
                                                                        ? StatusCode.SequenceError
                                                                        : StatusCode.SyntaxError);
                                                                errorCounter++;
                                                                if (errorCounter > 3)
                                                                {
                                                                    continuePlay = false;
                                                                    StatusWriter(writer, StatusCode.ConnectionLost);
                                                                    Logger.AddToLog(StatusCode.ConnectionLost
                                                                        .GetDescription());
                                                                    break;
                                                                }

                                                                continue;
                                                            }
                                                        }
                                                    }

                                                    if (!continuePlay) break;
                                                    //if (!hit)
                                                    //{
                                                    //    //writer.WriteLine(StatusCode.Miss.GetDescription());
                                                    //    Logger.AddToLog(StatusCode.Miss.GetDescription());
                                                    //}

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
                                                    if (errorCounter > 3)
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
                                                    writer.WriteLine(
                                                        "Thank you for playing. Don't chicken out next time!!");
                                                    Logger.AddToLog(StatusCode.ConnectionLost.GetDescription());
                                                    continuePlay = false;
                                                    break;
                                                }
                                                else if (command.StartsWith("270"))
                                                {
                                                    Logger.AddToLog(StatusCode.ConnectionLost.GetDescription());
                                                    continuePlay = false;
                                                    break;
                                                }
                                                else if (command.StartsWith("FIRE ",
                                                             StringComparison.InvariantCultureIgnoreCase) &&
                                                         FireSyntaxCheck(command))
                                                {
                                                    #region // Check if previously fired at

                                                    if (Opponent.CheckFiredAt(command))
                                                    {
                                                        //Logger.AddToLog($"Client: {StatusCode.SequenceError.GetDescription()}");
                                                        writer.WriteLine(StatusCode.SequenceError.GetDescription());
                                                        errorCounter++;
                                                        if (errorCounter > 3)
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
                                                else if (command.StartsWith("HELP",
                                                    StringComparison.InvariantCultureIgnoreCase))
                                                {
                                                    writer.WriteLine(
@"************************************************************
Write QUIT to terminate connection.
Write Fire <Coordinate> to fire.
IF opponent misses your boats, write '230 <Message>'.
If your opponent HIT your Carrier, write '241 <Message>'.
If your opponent HIT your Battleship, write '242 <Message>'.
If your opponent HIT your Destroyer, write '243 <Message>'.
If your opponent HIT your Submariner, write '244 <Message>'.
If your opponent HIT your Patrol Boat, write '245 <Message>'.

If your opponent SUNK your Carrier, write '251 <Message>'.
If your opponent SUNK your Battleship, write '252 <Message>'.
If your opponent SUNK your Destroyer, write '253 <Message>'.
If your opponent SUNK your Submariner, write '254 <Message>'.
If your opponent SUNK your Patrol Boat, write '255 <Message>'.

If your opponent wins, write '260 <Message>'
************************************************************");
                                                }
                                                else
                                                {
                                                    var syntax = SyntaxCheck(command);
                                                    if (!syntax)
                                                    {
                                                        StatusWriter(writer, StatusCode.SyntaxError);
                                                    }
                                                    else if (!FireSyntaxCheck(command))
                                                    {
                                                        StatusWriter(writer, StatusCode.SyntaxError);
                                                    }
                                                    else
                                                    {
                                                        StatusWriter(writer, StatusCode.SequenceError);
                                                    }

                                                    errorCounter++;
                                                    if (errorCounter > 3)
                                                    {
                                                        continuePlay = false;
                                                        StatusWriter(writer, StatusCode.ConnectionLost);
                                                        Logger.AddToLog(StatusCode.ConnectionLost.GetDescription());
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    if (!continuePlay)
                                        break;
                                }
                            }
                        }
                    }
                }
                catch (System.IO.IOException e)
                {
                    Logger.AddToLog(StatusCode.ConnectionLost.GetDescription());
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
            var commands = new List<string>() {"HELO", "START", "FIRE", "HELP"};

            if (!commands.Contains(input))
                return false;
            return true;
        }

        private void StatusWriter(StreamWriter writer, StatusCode code)
        {
            writer.WriteLine(code.GetDescription());
        }

        public void RestartServer()
        {
            try
            {
                listener?.Stop();                
                Logger.ClearLog();
                var manager = new WindowManager();
                manager.ShowWindow(new ShellViewModel());
                Application.Current.Windows[0].Close();
            }
            catch (Exception e)
            {
                Logger.AddToLog(StatusCode.ConnectionLost.GetDescription());
                Logger.AddToLog("Press Restart");
            }
        }
    }
}
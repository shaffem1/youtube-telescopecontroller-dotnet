using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;
using System.Threading;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
using System.Diagnostics;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace BasicApi

{
    class TriviaBot
    {
        Timer Timer1;
        Timer Timer2;
        Timer Timer3;
        Timer Timer4;
        Timer Timer5;
        String nextpagetoken; // ?
        int stage = 0;
        int done = 1;
        String currentQuestion = "0";
        String currentAnswer = "0";
        int questionAnswered = 0;
        DateTime askTime;
        String hint1 = "";
        String msgToSend = "";
        int startUpMsgHoldBack = 1; // Prevents messages from being sent to channel during first few seconds of program. Avoid flood of messages from reading pages. 
        int currentQuestionLine;
        string[,] namesValues = new string[50, 2];
        bool voteInProgress = false;
        int[] vote = new int[5];
        int timeBetweenStopWatchInitialStart = 0;
        int voteDurationTimeLimit = 20000; // Time to wait for votes on where to pan camera
        System.Diagnostics.Stopwatch timeBetweenMovesStopWatch = new System.Diagnostics.Stopwatch();
        List<String> recentMessages1 = new List<string>(new string[200]);
        List<String> recentVotes1 = new List<string>(new string[50]);
        int firstRun = 1;
        /*
        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("Trivia Bot");
            Console.WriteLine("==================================");
            TriviaBot crap = new TriviaBot();
            crap.start();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
        */
        public async void holdVote()
        {
            voteInProgress = true;
            vote[0] = 0;
            vote[1] = 0;
            vote[2] = 0;
            vote[3] = 0;
            vote[4] = 0;
            for (int i = 0; i < 50; i++)
            {
                recentVotes1[i] = "";
            }
            timeBetweenMovesStopWatch.Stop();
            Timer5.Change(25000, 25000);
            sendMsg("Type the number of the runway to move to. Example, 1, 15, 33, 19, or middle.");
        }

        public void checkVoteNeededforMove()
        {
            if (timeBetweenMovesStopWatch.Elapsed.TotalMilliseconds > 600000 && voteInProgress == false)
            {
                holdVote();
            }
            else
            {
                string blah = timeBetweenMovesStopWatch.ElapsedMilliseconds.ToString();
                int blah2 = Int32.Parse(blah);
                int timeleft = (600000 - blah2) / 1000;
                sendMsg(timeleft + " seconds left until next possible vote. ");
            }

        }

        public void addRecordtoDB(int location)
        {

            string connStr = "server=localhost;user=admin;database=cam;port=3306;password=Sanyo529";
            MySqlConnection conn = new MySqlConnection(connStr);
            try
            {
                Console.WriteLine("Connecting to MySQL...");
                conn.Open();

                string sql = "INSERT INTO todo (moveto) VALUES ('1')";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            conn.Close();
            Console.WriteLine("Done.");
        }

        public void addVoteRecordtoDB(string username, int vote)
        {

            string connStr = "server=localhost;user=admin;database=cam;port=3306;password=Sanyo529";
            MySqlConnection conn = new MySqlConnection(connStr);
            try
            {
                Console.WriteLine("Connecting to MySQL...");
                conn.Open();
                DateTime now = DateTime.UtcNow;
                DateTime then = new DateTime(1970, 1, 1);
                now.Subtract(then);
                Console.WriteLine(now);
                string sql = "INSERT INTO votes (username,vote) VALUES ('" + username + "','" + vote + "')";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            conn.Close();
            Console.WriteLine("Done.");
        }
        public string getScores()
        {
            string[] Entry = File.ReadAllLines("c:\\triviabot\\scores.txt");
            var orderedEntries = Entry.OrderByDescending(x => int.Parse(x.Split(',')[1]));

            var myList = orderedEntries.Take(5);
            String highScores = "High scores: ";
            foreach (var score in myList)
            {
                highScores += score + " | ";
            }

            return highScores;
        }

        void stackEm(String input1)
        {
            recentMessages1.Insert(0, input1);
        }

        void stackEmVoters(String input1)
        {
            recentVotes1.Insert(0, input1);
        }

        void addQuestion(String newQuestion)
        {
            string question = newQuestion.Remove(0, 5);
            File.AppendAllText("c:\\triviabot\\qa.txt", question + Environment.NewLine);

        }

        static void lineChanger(string newText, string fileName, int line_to_edit)
        {
            string[] arrLine = File.ReadAllLines(fileName);
            arrLine[line_to_edit] = newText;
            File.WriteAllLines(fileName, arrLine);
        }

        public int getUserScore(String userName)
        {
            int theirScore = 0;
            int counter = 0;
            string myLine = "";
            System.IO.StreamReader file =
            new System.IO.StreamReader("c:\\triviabot\\scores.txt");

            while ((myLine = file.ReadLine()) != null)
            {
                if (myLine.Contains(userName))
                {
                    string[] values = myLine.Split(',');
                    Int32.TryParse(values[1], out theirScore);
                }
                counter++;
            }
            file.Close();

            return theirScore;
        }
        public void addPoint(String userName)
        {
            int temp2 = 0;
            int counter = 0;
            int foundLine = -1;
            string myLine = "";
            String replacement = "";
            String newUserLine = "";
            int destinationLine = 0;
            System.IO.StreamReader file =
            new System.IO.StreamReader("c:\\triviabot\\scores.txt");

            while ((myLine = file.ReadLine()) != null)
            {
                if (myLine.Contains(userName))
                {
                    foundLine = counter;
                    string[] values = myLine.Split(',');
                    Int32.TryParse(values[1], out temp2);
                    temp2++;
                    replacement = values[0] + "," + temp2;
                }
                counter++;
            }
            file.Close();

            if (foundLine == -1)
            {
                newUserLine = userName + "," + "1";
                destinationLine = counter + 1;
                File.AppendAllText("c:\\triviabot\\scores.txt", newUserLine + Environment.NewLine);
            }
            else
            {
                lineChanger(replacement, "c:\\triviabot\\scores.txt", foundLine);
                foundLine = -1;
            }
        }
        public void generateHints()
        {

            StringBuilder sb = new StringBuilder(currentAnswer);

            for (int i = 1; i < sb.Length - 1; i++)
            {
                sb[i] = '_';
            }
            hint1 = sb.ToString();

        }

        public void getQuestion()
        {
            var lines = File.ReadAllLines("c:\\triviabot\\questions.txt");
            var r = new Random();
            var randomLineNumber = r.Next(0, lines.Length - 1);
            var line = lines[randomLineNumber];
            currentQuestion = line;
            currentQuestionLine = randomLineNumber;
        }

        public void getAnswer()
        {
            var answerLines = File.ReadAllLines("c:\\triviabot\\answers.txt");
            currentAnswer = answerLines[currentQuestionLine];
        }

        void Timer1_Tick(object state)
        {
            try
            {
                getMsg(currentAnswer);
                // Console.WriteLine("Time between moves timer: " + timeBetweenMovesStopWatch.ElapsedMilliseconds.ToString());
                //Console.WriteLine(DateTime.Now + " getting new messages");
            }
            catch (AggregateException ex)
            {
                foreach (var e in ex.InnerExceptions)
                {
                    Console.WriteLine("Error: " + e.Message);
                }
            }
            GC.Collect();
            Thread.Sleep(500);
        }

        void Timer3_Tick(object state)
        {
            try
            {
                startUpMsgHoldBack = 0;
                Console.WriteLine("Messages now allowed to be sent.");
                Timer3.Change(Timeout.Infinite, Timeout.Infinite);
            }
            catch (AggregateException ex)
            {
                foreach (var e in ex.InnerExceptions)
                {
                    Console.WriteLine("Error: " + e.Message);
                }
            }
            GC.Collect();
            Thread.Sleep(500);
        }

        void Timer5_Tick(object state)
        {
            if (vote[0] == 0 && vote[1] == 0 && vote[2] == 0 && vote[3] == 0 && vote[4] == 0)
            {
                voteInProgress = false;
                sendMsg("No votes were collected. Camera will stay at current position.");
            }
            else
            {
                voteInProgress = false;
                int highestVotes = vote.Max();
                int maxIndex = vote.ToList().IndexOf(highestVotes);
                string winner = "";
                if (maxIndex == 0) winner = "15";
                if (maxIndex == 1) winner = "19";
                if (maxIndex == 2) winner = "1";
                if (maxIndex == 3) winner = "33";
                if (maxIndex == 4) winner = "middle";
                Timer5.Change(Timeout.Infinite, Timeout.Infinite);
                timeBetweenMovesStopWatch.Restart();
                sendMsg("Winning vote was " + winner + " with " + vote[maxIndex] + " votes. Moving camera to " + winner + ".");
                GlobalVariables.winningVote = maxIndex;
            }
        }
        
        void Timer4_Tick(object state)
        {
            try
            {
                //sendMsg("Type !movecam to move the camera to another runway view.");
            }
            catch (AggregateException ex)
            {
                foreach (var e in ex.InnerExceptions)
                {
                    Console.WriteLine("Error: " + e.Message);
                }
            }
            GC.Collect();
            Thread.Sleep(500);
        }
        void Timer2_Tick(object state)
        {
            if (done == 1)
            {
                getQuestion();
                getAnswer();
                generateHints();
                stage = 0;
                done = 0;
                questionAnswered = 0;
            }
            else if (stage == 0 && done != 1)
            {
                stage++;
                askTime = DateTime.Now;
                try
                {
                    msgToSend = currentQuestion + "?";
                    sendMsg(msgToSend);
                }
                catch (AggregateException ex)
                {
                    foreach (var e in ex.InnerExceptions)
                    {
                        Console.WriteLine("Error: " + e.Message);
                    }
                }
                GC.Collect();
                Thread.Sleep(500);
            }
            else if (stage == 1 && done != 1)
            {
                stage++;
                try
                {
                    //  sendMsg("hint 1");
                }
                catch (AggregateException ex)
                {
                    foreach (var e in ex.InnerExceptions)
                    {
                        Console.WriteLine("Error: " + e.Message);
                    }
                }
                GC.Collect();
                Thread.Sleep(500);
            }
            else if (stage == 2 && done != 1)
            {
                stage++;
                try
                {
                    sendMsg("Hint: " + hint1);
                }
                catch (AggregateException ex)
                {
                    foreach (var e in ex.InnerExceptions)
                    {
                        Console.WriteLine("Error: " + e.Message);
                    }
                }
                GC.Collect();
                Thread.Sleep(500);
            }
            else if (stage == 3 && done != 1)
            {
                stage++;
                try
                {
                    //  sendMsg("hint 3");
                }
                catch (AggregateException ex)
                {
                    foreach (var e in ex.InnerExceptions)
                    {
                        Console.WriteLine("Error: " + e.Message);
                    }
                }
                GC.Collect();
                Thread.Sleep(500);
            }
            else if (stage == 4 && done != 1)
            {
                stage = 0;
                done = 1;
                try
                {
                    sendMsg("Time is up! The correct answer was:  " + currentAnswer);
                }
                catch (AggregateException ex)
                {
                    foreach (var e in ex.InnerExceptions)
                    {
                        Console.WriteLine("Error: " + e.Message);
                    }
                }
                GC.Collect();
                Thread.Sleep(500);
            }
        }

        public async Task initializeGoogleConnection()
        {
            UserCredential credential;

            using (var stream = new FileStream("c:\\triviabot\\client_secrets.json", FileMode.Open, FileAccess.Read))
            {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    // This OAuth 2.0 access scope allows for full read/write access to the
                    // authenticated user's account.
                    new[] { YouTubeService.Scope.Youtube },
                    "user",
                    CancellationToken.None,
                    new FileDataStore(this.GetType().ToString())
                );
            }
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = this.GetType().ToString()
            });
        }

        public void start()
        {
            /* April 25, 2019 commented out to speed up tracker
            if(firstRun == 1)
            {
                firstRun = 0;
                initializeGoogleConnection();
                Thread.Sleep(5000); 
            }
            Timer1 = new Timer(Timer1_Tick, null, 3000, 2000);  // Delay for retrieving channel chat messages 
            Timer3 = new Timer(Timer3_Tick, null, 25000, 25000); // Delay between stages of asking questsion, hint and giving answer
            Timer4 = new Timer(Timer4_Tick, null, 600000, 10800000); // How often advertises commands
            Timer5 = new Timer(Timer5_Tick, null, Timeout.Infinite, Timeout.Infinite);
            if (timeBetweenStopWatchInitialStart == 0)
            {
                timeBetweenStopWatchInitialStart = 1;
                timeBetweenMovesStopWatch.Restart();
                Console.WriteLine("Time between moves StopWatch started.");
            }
            */
        }

        public async Task sendMsg(string myMessage)
        {
            if (startUpMsgHoldBack == 0)
            {
                Console.WriteLine("SENT " + myMessage);
                UserCredential credential;

                using (var stream = new FileStream("c:\\triviabot\\client_secrets.json", FileMode.Open, FileAccess.Read))
                {
                    credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.Load(stream).Secrets,
                        // This OAuth 2.0 access scope allows for full read/write access to the
                        // authenticated user's account.
                        new[] { YouTubeService.Scope.Youtube },
                        "user",
                        CancellationToken.None,
                        new FileDataStore(this.GetType().ToString())
                    );
                }
                var youtubeService = new YouTubeService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = this.GetType().ToString()
                });
                LiveChatMessage comments = new LiveChatMessage();
                LiveChatMessageSnippet mySnippet = new LiveChatMessageSnippet();
                LiveChatTextMessageDetails txtDetails = new LiveChatTextMessageDetails();
                txtDetails.MessageText = myMessage;
                mySnippet.TextMessageDetails = txtDetails;
                mySnippet.LiveChatId = "x";     // main channel
                //mySnippet.LiveChatId = "x";  //cam 3
                mySnippet.Type = "textMessageEvent";
                comments.Snippet = mySnippet;
                comments = await youtubeService.LiveChatMessages.Insert(comments, "snippet").ExecuteAsync();
            }
            else
            {
                Console.WriteLine("HELD BACK " + myMessage);
            }
        }

        public async Task getMsg(String curAnswer)
        {
            UserCredential credential;
            using (var stream = new FileStream("c:\\triviabot\\client_secrets.json", FileMode.Open, FileAccess.Read))
            {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    // This OAuth 2.0 access scope allows for full read/write access to the
                    // authenticated user's account.
                    new[] { YouTubeService.Scope.Youtube },
                    "user",
                    CancellationToken.None,
                    new FileDataStore(this.GetType().ToString())
                );
            }
            var ytService = new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = this.GetType().ToString()
            });

            String liveChatId = "x"; //main channel
           // String liveChatId = "x"; // cam 3
            var chatMessages = ytService.LiveChatMessages.List(liveChatId, "id,snippet,authorDetails");
            chatMessages.PageToken = nextpagetoken;
            var chatResponse = await chatMessages.ExecuteAsync();
            nextpagetoken = chatResponse.NextPageToken;
            //Console.WriteLine("nextpagetoken is " + nextpagetoken);
            long? pollinginterval = chatResponse.PollingIntervalMillis;
            PageInfo pageInfo = chatResponse.PageInfo;
            List<LiveChatMessageListResponse> messages = new List<LiveChatMessageListResponse>();
            //Console.WriteLine(chatResponse.PageInfo.TotalResults + " total messages " + chatResponse.PageInfo.ResultsPerPage + " results per page" + nextpagetoken);

            foreach (var chatMessage in chatResponse.Items)
            {
                string messageId = chatMessage.Id;
                string displayName = chatMessage.AuthorDetails.DisplayName;
                string displayMessage = chatMessage.Snippet.DisplayMessage;
                System.DateTime messageTime = chatMessage.Snippet.PublishedAt.Value;
                var now = DateTime.Now;
                var timeSince = now - messageTime;
                int toSeconds = timeSince.Seconds;
                //Console.WriteLine(DateTime.Now + "   msg time: " + messageTime + "  ago: " + timeSince);
                Console.WriteLine("MessageID:" + messageId + "  " + displayMessage);
                //Console.WriteLine(recentMessages.Contains(messageId).Equals(false));

                // && toSeconds < 33 && toSeconds > 25 

                if (displayName != "Trivia Bot" && recentMessages1.Contains(messageId).Equals(false) && startUpMsgHoldBack == 0)
                {
                    stackEm(messageId);
                    Console.WriteLine("recent message: " + messageTime + " Delay: " + toSeconds + "  " + displayMessage);

                    // if (displayMessage.Contains(curAnswer) && done == 0 && questionAnswered == 0)
                    if ((displayMessage.IndexOf(curAnswer, StringComparison.OrdinalIgnoreCase) >= 0) && done == 0 && questionAnswered == 0)
                    {
                        questionAnswered = 1;
                        done = 1;
                        String output1 = "You got it, " + displayName + "! [" + toSeconds + "secs] The correct answer was: " + curAnswer + ".";
                        sendMsg(output1);
                        addPoint(displayName);
                    }
                    else

                     if (displayMessage.Contains("!trivia"))
                    {
                        done = 1;
                        stage = 0;// necessary?
                        String msg = "Trivia Bot started! First question coming up...";
                        sendMsg(msg);
                        Timer2 = new Timer(new TimerCallback(Timer2_Tick), null, 0, 10000);
                    }
                    else

                     if (displayMessage.Contains("!stop"))
                    {
                        done = 1;
                        String msg = "Trivia Stopped by " + displayName;
                        sendMsg(msg);
                        Timer2.Change(Timeout.Infinite, Timeout.Infinite);
                    }
                    else

                     if (displayMessage.Contains("!myscore"))
                    {
                        int s1 = getUserScore(displayName);
                        String msg = displayName + "'s score: " + s1;
                        sendMsg(msg);
                    }
                    else

                     if (displayMessage.Contains("!add"))
                    {
                        addQuestion(displayMessage);
                        String msg = "Question added.";
                        sendMsg(msg);
                    }
                    else
                    
                     if (displayMessage.Contains("!highscores"))
                    {
                        string t4 = getScores();
                        sendMsg(t4);
                    }
                    else
                     if (displayMessage.Contains("!movecam"))
                    {
                        //checkVoteNeededforMove();
                    }
                    else
                     if (recentVotes1.Contains(displayName).Equals(false) && voteInProgress == true)
                    {
                        if (displayMessage.Contains("15"))
                        {
                            vote[0] += 1;
                            sendMsg(displayName + " voted for 15");
                            stackEmVoters(displayName);
                         //   addVoteRecordtoDB(displayName, 15);
                        } else
                        if (displayMessage.Contains("19"))
                        {
                            vote[1] += 1;
                            sendMsg(displayName + " voted for 19");
                            stackEmVoters(displayName);
                        //    addVoteRecordtoDB(displayName, 19);
                        }
                        else
                        if (displayMessage.Contains("1"))
                        {
                            vote[2] += 1;
                            sendMsg(displayName + " voted for 1");
                            stackEmVoters(displayName);
                         //   addVoteRecordtoDB(displayName, 1);
                        }
                        else
                        if (displayMessage.Contains("33"))
                        {
                            vote[3] += 1;
                            sendMsg(displayName + " voted for 33");
                            stackEmVoters(displayName);
                         //   addVoteRecordtoDB(displayName, 33);
                        }
                        else
                        if (displayMessage.Contains("middle"))
                        {
                            vote[4] += 1;
                            sendMsg(displayName + " voted for middle");
                            stackEmVoters(displayName);
                           // addVoteRecordtoDB(displayName, 5); //using 5 for the middle area since no runway number
                        }
                    }
                }
            }
        }
    }
}


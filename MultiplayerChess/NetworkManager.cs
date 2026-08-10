using MultiplayerUtil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Mathematics;
using UnityEngine;

namespace Multiplayer_Chess
{
    public class NetworkManager
    {
        public static void Setup()
        {
            ObserveManager.SubscribeToType(typeof(ChessMoveData), out Callbacks.SenderUnityEvent onReceived);

            onReceived.AddListener(payload =>
            {
                var obj = Data.Deserialize<ChessMoveData>(payload.Item1);
                var sender = payload.Item2;

                ReceiveChessData(obj);
            });
        }

        public static void Host()
        {
            LobbyManager.CreateLobby("Chess lobby epik", 2, false, false, false, ("Multiplayer_Chess", "chess"));

            Plugin.Instance.StartCoroutine(WaitForLobby());
        }

        public static void OnLeave()
        {
            LobbyManager.Disconnect();
        }

        public static void Join()
        {
            string clipboardText = GUIUtility.systemCopyBuffer;

            if (ulong.TryParse(clipboardText, out ulong lobbyId))
            {
                LobbyManager.JoinLobbyWithID(lobbyId);
                Plugin.Instance.StartCoroutine(WaitForLobby(false));
            }
            else
            {
                HudMessageReceiver.Instance.SendHudMessage($"Invalid Ulong: {clipboardText}");
                Debug.LogError($"Clipboard does not contain a valid ulong ({clipboardText}).");
            }
        }

        static IEnumerator WaitForLobby(bool created = true)
        {
            yield return new WaitUntil(() => LobbyManager.current_lobby.HasValue);
            ulong lobbyId = LobbyManager.current_lobby.Value.Id;
            string text = created ? "Lobby created with ID: " : "Lobby joined with ID: ";
            HudMessageReceiver.Instance.SendHudMessage($"{text} {lobbyId}");

            if (created) GUIUtility.systemCopyBuffer = lobbyId.ToString();
        }

        public static void SendChessData(ChessMoveData data)
        {
            if (!LobbyManager.current_lobby.HasValue) return;
            LobbyManager.SendData(data);
        }

        public static void ReceiveChessData(ChessMoveData data)
        {
            ChessManager manager = MonoSingleton<ChessManager>.Instance;
            if (manager == null) return;

            int2 start = new int2(data.fromX, data.fromY);
            int2 end = new int2(data.toX, data.toY);

            manager.GetLegalMoves(start);
            List<ChessManager.MoveData> legalMoves = manager.legalMoves;

            ChessManager.MoveData? match = null;
            foreach (ChessManager.MoveData candidate in legalMoves)
            {
                if (candidate.EndPosition.Equals(end))
                {
                    match = candidate;
                    break;
                }
            }

            if (!match.HasValue)
            {
                Plugin.Logger.LogError($"Received move desync.");
                return;
            }

            // Convert coordinates to readable text (e.g., e2 to e4)
            char startFile = (char)('a' + data.fromX);
            int startRank = data.fromY + 1;
            char endFile = (char)('a' + data.toX);
            int endRank = data.toY + 1;

            // Notify player of opponent's move and that it is now their turn
            HudMessageReceiver.Instance.SendHudMessage($"Opponent played: {startFile}{startRank} -> {endFile}{endRank}. Your turn!");

            ChessPatchManager.ApplyMovementsOfOtherSide = true;
            manager.MakeMove(match.Value, true);
            ChessPatchManager.ApplyMovementsOfOtherSide = false;
        }
    }

    public class ChessMoveData
    {
        public byte fromX;
        public byte fromY;
        public byte toX;
        public byte toY;

        public ChessMoveData(int fromX, int fromY, int toX, int toY)
        {
            this.fromX = (byte)fromX;
            this.fromY = (byte)fromY;
            this.toX = (byte)toX;
            this.toY = (byte)toY;
        }
    }
}
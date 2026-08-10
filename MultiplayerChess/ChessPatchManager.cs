using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace Multiplayer_Chess
{
    [HarmonyPatch]
    public class ChessPatchManager
    {
        public static bool ApplyMovementsOfOtherSide;

        [HarmonyPatch(typeof(ChessManager), nameof(ChessManager.MakeMove))]
        public static void Postfix(ChessManager __instance, ChessManager.MoveData moveData, bool updateVisuals)
        {
            if (!updateVisuals || ApplyMovementsOfOtherSide) return;

            NetworkManager.SendChessData(new ChessMoveData(moveData.StartPosition.x, moveData.StartPosition.y, moveData.EndPosition.x, moveData.EndPosition.y));
        }
    }
}
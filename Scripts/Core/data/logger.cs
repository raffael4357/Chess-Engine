using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public static class logger
{
    const string path = "./log.txt";

    static bool hasRun = false;

    // logging a string to the log file
    public static void Log(string data)
    {
        if (!hasRun)
        {
            File.WriteAllText(path, string.Empty);
        }

        StreamWriter writer = new StreamWriter(path, true);
        writer.WriteLine(data);
        writer.Close();

        hasRun = true;
    }

    // logging a position to the log file
    public static void LogPosition(gameState game)
    {
        if (!hasRun)
        {
            File.WriteAllText(path, string.Empty);
        }

        StreamWriter writer = new StreamWriter(path, true);

        writer.Write(writer.NewLine);
        for (int i = 0, n = game.pieces.GetLength(0); i < n; i++)
        {
            for (int j = 0, o = game.pieces.GetLength(1); j < o; j++)
            {
                writer.Write(GetPieceCharacter(game.pieces[j, i]) + " ");
            }
            writer.Write(writer.NewLine);
        }
        writer.Write(writer.NewLine);

        writer.Close();

        hasRun = true;
    }

    // getting the corresponding ascii characters for every piece
    static char GetPieceCharacter(piece piece)
    {
        switch (piece.type)
        {
            case board.king:
                return piece.isWhite ? 'K' : 'k';
            case board.queen:
                return piece.isWhite ? 'Q' : 'q';
            case board.rook:
                return piece.isWhite ? 'R' : 'r';
            case board.bishop:
                return piece.isWhite ? 'B' : 'b';
            case board.knight:
                return piece.isWhite ? 'N' : 'n';
            case board.pawn:
                return piece.isWhite ? 'P' : 'p';
            default:
                return '-';
        }
    }
}

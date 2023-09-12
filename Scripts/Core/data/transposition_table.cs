using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class transposition_table
{
    entry[] table;
    public const byte exactValue = 0;
    public const byte alphaValue = 1;
    public const byte betaValue = 2;

    public void Store(ulong hash, move move, int depth, int evaluation, byte evalType)
    {
        // adding the values to our hashtable
        entry newEntry = new entry(true);
        newEntry.hashKey = hash;
        newEntry.bestMove = move;
        newEntry.searchDepth = depth;
        newEntry.evaluation = evaluation;
        newEntry.nodeType = (byte)evalType;

        table[hash % (ulong)table.Length] = newEntry;
    }

    public entry Get(ulong hash)
    {
        // returning the values in our hashtable if they actually belong to the
        // current position (overlaps are frequent)
        entry storedEntry = table[hash % (ulong)table.Length];

        if (storedEntry.hashKey == hash && storedEntry.valid)
        {
            return storedEntry;
        }
        else
        {
            return new entry(false);
        }
    }

    public transposition_table(int sizeInBytes)
    {
        // constructor for our transposition table
        // we calculate the size of our array by looking at the size of an entry
        // and the user specified size in bytes
        int entryInBytes = Marshal.SizeOf<entry>();
        int numEntries = (int)Mathf.Floor(sizeInBytes / entryInBytes);

        logger.Log("Size of transposition table: " + numEntries);
        table = new entry[numEntries];
    }

    // struct for an entry to the transposition table
    public struct entry
    {
        public ulong hashKey;
        public move bestMove;
        public int searchDepth;
        public int evaluation;
        public bool valid;
        public byte nodeType;

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (!(obj is entry))
            {
                return false;
            }
            return ((entry)obj).hashKey == this.hashKey && ((entry)obj).bestMove.Equals(this.bestMove) && ((entry)obj).searchDepth == this.searchDepth && ((entry)obj).evaluation == this.evaluation && ((entry)obj).valid == this.valid && ((entry)obj).nodeType == this.nodeType;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public entry(bool valid)
        {
            this.valid = valid;
            this.hashKey = 0;
            this.bestMove = new move();
            this.searchDepth = 0;
            this.evaluation = 0;
            this.nodeType = exactValue;
        }
    }
}

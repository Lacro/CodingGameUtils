using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

// faire un graphe avec les chemins en arrete

public class InitMap {
    public int nbCells;
    public int[][] neigs;
    public int[] res;
    public int[] type;
    public List<int> myBases;
    public List<int> oppBases;

    public InitMap(int nbCells) {
        this.nbCells = nbCells;
        neigs = new int[nbCells][];
        res = new int[nbCells];
        type = new int[nbCells];
        myBases = new();
        oppBases = new();
    }
}

public class Trip {
    List<Cell> _path;
    public int _dist;
    Cell _end;

    public Trip (Cell start, Cell end) {
        _path = new();
        _path.Add(start);
        _end = end;
    }
    public Cell Start() { return _path.First(); }
    public Cell End() { return _end; }
    public int Length() { return _path.Count + 1; }
    public List<Cell> Path() {
        List<Cell> path = new (_path);
        path.Add(_end);
        return path;
    }
}

public class Coord {
    public int x,y,z;
    public Coord() {}
    public Coord (Coord coord) {
        x = coord.x;
        y = coord.y;
        z = coord.z;
    }
    public Coord GetNeig(int dir) {
        Coord newCoord = new Coord(this);
        switch (dir) {
            case 0: newCoord.z--; newCoord.x++; break;
            case 1: newCoord.y++; newCoord.z--; break;
            case 2: newCoord.x--; newCoord.y++; break;
            case 3: newCoord.z++; newCoord.x--; break;
            case 4: newCoord.y--; newCoord.z++; break;
            case 5: newCoord.x++; newCoord.y--; break;
            default : break;
        }
        return newCoord;
    }
    public int GetDist(Coord other) { return Math.Max(Math.Max(Math.Abs(other.x-x), Math.Abs(other.y-y)), Math.Abs(other.z-z)); }
}

public class Cell {
    private Map _map;
    public enum Type { Empty = 0, Egg = 1, Crystal = 2 };

    private int _index;
    private Coord _coord;
    private Type _type;
    private int _nbRes, _nbMyAnts, _nbOppAnts;
    private Cell[] _neigs;
    
    public Cell(Map map, int index, Type type, int resources) {
        _map = map;
        _index = index;
        _type = type;
        _nbRes = resources;
        _nbMyAnts = 0;
        _nbOppAnts = 0;
        _coord = null;
    }
    public void InitNeigs(int[] neigsIndex) {
        _neigs = new Cell[8];
        for (int i =0; i < neigsIndex.Count(); i++)
            if (neigsIndex[i] >= 0)
                _neigs[i]  = _map._cells[neigsIndex[i]];
            else
                _neigs[i]  = null;
    }
    public void InitCoord(Coord coord) {
        _coord = coord;
        for (int i = 0; i < _neigs.Count(); i++) {
            if (_neigs[i] != null && _neigs[i]._coord == null) {
                _neigs[i].InitCoord(coord.GetNeig(i));
            }
        }
        //Console.Error.WriteLine($"coor:{_coord.x}, {_coord.y}, {_coord.z}");
    }
    public void SetResources(int quantity) { 
        _nbRes = quantity;
        if (_nbRes == 0)
            _type = Type.Empty;
    }
    public void SetMyAnts (int quantity) { _nbMyAnts = quantity; }
    public void SetOppAnts(int quantity) { _nbOppAnts = quantity; }

    public int  GetIndex()          { return _index; }
    public Coord GetCoord()         { return _coord; }
    public Type GetCellType()       { return _type; }
    public int GetNbRes()           { return _nbRes; }
    public int GetMyAnts()          { return _nbMyAnts; }
    public int GetOppAnts()         { return _nbOppAnts; }
    public Cell[] GetNeigs()        { return _neigs; }
    public int GetCost()            { return _type == Type.Empty ? 1 : 0; }
    public int GetDist(Cell other)  { return _coord.GetDist(other.GetCoord()); }
}

public class Map {
    public Cell[] _cells;
    public List<Cell> _myBases, _oppBases;
    public int _initialNbEggCell, _initialNbEgg, _initialNbCrystalCell, _initialNbCrystal;

    public Map(InitMap initMap) {
        _cells = new Cell [initMap.nbCells];
        for (int i = 0; i < initMap.nbCells; i++)
            _cells[i] = new Cell(this, i, (Cell.Type)initMap.type[i], initMap.res[i]);
        
        for (int i = 0; i < initMap.nbCells; i++)
            _cells[i].InitNeigs(initMap.neigs[i]);
        _cells[0].InitCoord(new Coord(){ x=0, y=0, z=0 });
        
        _myBases = new();
        foreach (int cell in initMap.myBases)
            _myBases.Add(_cells[cell]);

        _oppBases = new();
        foreach (int cell in initMap.oppBases)
            _oppBases.Add(_cells[cell]);

        List<Cell> eggCells = GetResCells(Cell.Type.Egg);
        _initialNbEggCell = eggCells.Count;
        _initialNbEgg = eggCells.Sum(x => x.GetNbRes());

        List<Cell> crystalCells = GetResCells(Cell.Type.Crystal);
        _initialNbCrystalCell = crystalCells.Count;
        _initialNbCrystal = crystalCells.Sum(x => x.GetNbRes());
    }

    public void SetResources(int index, int quantity)  { _cells[index].SetResources(quantity); }
    public void SetMyAnts   (int index, int quantity)  { _cells[index].SetMyAnts(quantity); }
    public void SetOppAnts  (int index, int quantity)  { _cells[index].SetOppAnts(quantity); }

    public Cell GetBase(int index = 0)     { return _myBases[index]; }
    public int GetNbBase()                 { return _myBases.Count; }
    public int GetNbCells()                { return _cells.Count(); }
    public int GetNbRes(Cell.Type type)    { return GetResCells(type).Sum(x => x.GetNbRes()); }
    public double GetPropEggsConsumed()    { return 1 - (((double)GetNbRes(Cell.Type.Egg)) / (double)_initialNbEgg); }
    public double GetPropCrystalConsumed() { return 1 - (((double)GetNbRes(Cell.Type.Crystal)) / (double)_initialNbCrystal); }

    public List<Cell> GetResCells(Cell.Type type) {
        List<Cell> cells = new();
        foreach (Cell cell in _cells) {
            if (cell.GetCellType() == type && cell.GetNbRes() != 0) {
                cells.Add(cell);
            }
        }
        return cells;
    }

    // ressource la plus proche pour chaque base
    public List<Trip> GetShortestBaseTrips(Cell.Type type, List<Cell> ignore = null) {
        List<Trip> bestTrip = new();

        foreach (Cell mybase in _myBases) {
            Trip tmp = GetNearestResCell(type, mybase, ignore);
            if (tmp != null) {
                bestTrip.Add(tmp);
            }
        }

        return bestTrip;
    }

    //ressource la plus proche d'un point
    Trip GetNearestResCell(Cell.Type type, Cell start, List<Cell> ignore = null) {
        Trip trip = null;
        List<Cell> seen = new();
        Queue<Cell> q =new();

        if (ignore == null) 
            ignore = new();
        
        q.Enqueue(start);
        seen.Add(start);

        Cell tmp;
        while(trip == null && q.Count != 0) {
            tmp = q.Dequeue();

            if (tmp.GetCellType() == type && !ignore.Contains(tmp)) {
                trip = new Trip(start, tmp);
            }
            else {
                foreach (Cell neig in tmp.GetNeigs()) {
                    if (neig != null && !seen.Contains(neig)) {
                        q.Enqueue(neig);
                        seen.Add(neig);
                    }
                }
            }
        }
        
        return trip;
    }

    public List<Cell> GetPath (Cell from, Cell to) {
        List<Cell> trip = new();

        PriorityQueue<string, int> exp = new();
        List<Cell> viewed = new();

        //exp.Add(from.GetDist(to), from);
        exp.Enqueue("salut", 10);
        exp.Enqueue("hello", 2);
        exp.Enqueue("coucou", 11);

        while (exp.Count != 0) {
            Console.Error.WriteLine(exp.Dequeue());
            Cell cur;
        }

        return trip;
    }
}

class Player
{
    static void eggThenChristal(Map map) {
        List<string> cmds = new();
        double propEgg = 0.5;
        double propCrystal = 0.3;
         
        Console.Error.WriteLine($"consumed : {map.GetPropEggsConsumed()} : {map.GetPropCrystalConsumed()}");
        if (map.GetPropEggsConsumed() < propEgg && map.GetPropCrystalConsumed() < propCrystal) {
            if (map.GetNbBase() == 1) {
                List<Cell> eggs = new();
                int i = 1;
                do {
                    List<Trip> trip = map.GetShortestBaseTrips(Cell.Type.Egg, eggs);

                    if (trip.Count == 0) {
                        Console.Error.WriteLine($"travel not found: {eggs}");
                    }
                    else {
                        cmds.Add("LINE " + trip[0].Start().GetIndex() + " " + trip[0].End().GetIndex() + " 1");
                        eggs.Add(trip[0].End());
                    }
                    i++;
                } while(i < map.GetResCells(Cell.Type.Egg).Count && i < map._initialNbEggCell*propEgg);
            }
            else {
                foreach (Trip trip in map.GetShortestBaseTrips(Cell.Type.Egg)) {
                    cmds.Add("LINE " + trip.Start().GetIndex() + " " + trip.End().GetIndex() + " 1");
                }
            }

        }
        else {
            foreach (Cell cell in map.GetResCells(Cell.Type.Crystal)) {
                cmds.Add("LINE " + map.GetBase().GetIndex() + " " + cell.GetIndex() + " 1");
            }
        }
        
        Console.WriteLine(string.Join<string>(';',cmds));
    }

    static void tree(Map map) {
        int nbEggs = map.GetResCells(Cell.Type.Egg).Count() / 4;
        int nbCrytal = map.GetResCells(Cell.Type.Crystal).Count() / 4;


    }

    static void Main(string[] args)
    {
        Map map = null;
        string[] inputs;

        {
            InitMap initMap = new(int.Parse(Console.ReadLine()));

            for (int i = 0; i < initMap.nbCells; i++)
            {
                inputs = Console.ReadLine().Split(' ');

                initMap.type[i] = int.Parse(inputs[0]);
                initMap.res[i] = int.Parse(inputs[1]);

                initMap.neigs[i] = new int[8];
                for (int j = 0; j <= 5; j++)
                    initMap.neigs[i][j] = int.Parse(inputs[j + 2]); // the index of the neighbouring cell for each direction
            }

            int numberOfBases = int.Parse(Console.ReadLine());
            inputs = Console.ReadLine().Split(' ');
            for (int i = 0; i < numberOfBases; i++)
                initMap.myBases.Add(int.Parse(inputs[i]));

            inputs = Console.ReadLine().Split(' ');
            for (int i = 0; i < numberOfBases; i++)
                initMap.oppBases.Add(int.Parse(inputs[i]));
            
            map = new(initMap);
        }
        map.GetPath(null, null);
        while (true)
        {
            for (int i = 0; i < map.GetNbCells(); i++)
            {
                inputs = Console.ReadLine().Split(' ');
                map.SetResources(i, int.Parse(inputs[0])); // the current amount of eggs/crystals on this cell
                map.SetMyAnts(i, int.Parse(inputs[1])); // the amount of your ants on this cell
                map.SetOppAnts(i, int.Parse(inputs[2])); // the amount of opponent ants on this cell
            }

            eggThenChristal(map);

            
            // To debug: Console.Error.WriteLine("Debug messages...");
            // WAIT | LINE <sourceIdx> <targetIdx> <strength> | BEACON <cellIdx> <strength> | MESSAGE <text>
        }
    }
}
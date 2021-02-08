using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Clustering
{
    class ComboResult
    {
        public int ComboTypeID;
        public int UnitTypeID;
        public int index;
    }
    public class UnitInformation 
    {
        public int UnitTypeID;
        public double UnitTypeAreaSize;
        public bool bOccupied;
        public int index;
        public int Priority;

        public int ResidentID;
        public int ClusterID;
    }
    class ComboType
    {
        public int ComboTypeID;
        public int ClusterID;
        public bool bFull;
        public List<UnitInformation> UnitCombolst;

        public bool FindSuitableComboType(ResidentNode residentnode, ComboType[] ComboTypeList)
        {
            ComboResult combo = new ComboResult();
            combo.ComboTypeID = -1;
            combo.UnitTypeID = -1;
            combo.index = -1;
            bool breturn = false;
            // already assigned
            if (ComboTypeID >= 0)
            {
                foreach (var unit in UnitCombolst)
                {
                    if (unit.bOccupied == false)
                    {
                        if ((residentnode.originalArea <= unit.UnitTypeAreaSize + 0.1) && (residentnode.originalArea >= unit.UnitTypeAreaSize))   // found a resident to live
                        {
                            unit.bOccupied = true;
                            unit.ResidentID = residentnode.ResidentID;
                            unit.ClusterID = residentnode.ClusterID;
                            combo.UnitTypeID = unit.UnitTypeID;
                            combo.ComboTypeID = ComboTypeID;
                            breturn = true;
                        }
                    }
                }
            }
            else
            {
                // not assigned
                foreach (var combotype in ComboTypeList)
                {
                    if (combo.ComboTypeID < 0)
                    {
                        foreach (var unit in combotype.UnitCombolst)
                        {
                            // found a suitable combo Type
                            if ((combo.ComboTypeID < 0)&&((residentnode.originalArea <= unit.UnitTypeAreaSize + 0.1)&& (residentnode.originalArea >= unit.UnitTypeAreaSize)))
                            {
                                combo.ComboTypeID = combotype.ComboTypeID;
                                combo.UnitTypeID = unit.UnitTypeID;
                                combo.index = unit.index;
                                breturn = true;
                            }
                        }
                        if (combo.ComboTypeID >= 0)
                        {
                            ComboTypeID = combotype.ComboTypeID;
                            UnitCombolst = new List<UnitInformation>();
                            foreach (var unit in combotype.UnitCombolst)
                            {
                                UnitInformation newunit = new UnitInformation();
                                newunit.index = unit.index;
                                newunit.UnitTypeAreaSize = unit.UnitTypeAreaSize;
                                newunit.UnitTypeID = unit.UnitTypeID;
                                if (combo.index == unit.index)
                                {
                                    newunit.bOccupied = true;
                                    newunit.ResidentID = residentnode.ResidentID;
                                    newunit.ClusterID = residentnode.ClusterID;
                                }
                                UnitCombolst.Add(newunit);
                            }
                        }
                    }
                }
            }
            return breturn;
        }
    }
    class ResidentNode 
    {
        public int ClusterID;
        public double originalArea;
        public int ResidentID;
        public int ParcelID;
    }

    class ParcelNode : ComboType
    {
        public double angularDistance;
        public int ParcelID;
    }
}

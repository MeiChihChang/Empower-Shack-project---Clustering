using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Clustering
{
    public class ClusteringComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public ClusteringComponent()
          : base("Clustering", "Clustering",
              "Decoding Space/Clustering",
              "DecodingSpaces", "Extras")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Cluster Tag", "ClusterTag", "Cluster NameTag", GH_ParamAccess.list);
            pManager.AddTextParameter("Resident Name", "ResidentName", "Resident Name", GH_ParamAccess.list);
            pManager.AddNumberParameter("Preference Unit Area", "PreferenceUnitArea", "Preference Unit Area", GH_ParamAccess.list);
            pManager.AddNumberParameter("Unit Area Size", "Unit Area Size", "Unit Area Size", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Unit Priority", "Unit Priority", "Unit Priority", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Combo Bulding Type", "Combo Bulding Type", "Combo Bulding Type", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Shortest Angular Distance", "SAngularDistance", "Shorest Angular Distance", GH_ParamAccess.tree);
            pManager.AddGenericParameter("Street Network Generic Data", "SNetworkGData", "Street Network Generic Data", GH_ParamAccess.list);
            /// </summary>
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Cluster Tag", "ClusterTag", "Cluster NameTag", GH_ParamAccess.tree);
            pManager.AddTextParameter("Resident Name", "ResidentName", "Resident Name", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Combo Type", "ComboType", "Combo Building Type", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<String> inputClusterNameTagList = new List<String>();
            if ((!DA.GetDataList<String>(0, inputClusterNameTagList)))
                return;
            List<String> ResidentNameList = new List<String>();
            if ((!DA.GetDataList<String>(1, ResidentNameList)))
                return;
            List<double> OriginalAreaList = new List<double>();
            if ((!DA.GetDataList<double>(2, OriginalAreaList)))
                return;
            List<double> UnitAreaSizeList = new List<double>();
            if ((!DA.GetDataList<double>(3, UnitAreaSizeList)))
                return;
            int UnitPriority = 0;
            if ((!DA.GetData<int>(4, ref UnitPriority)))
                return;
            Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.GH_Integer> comboBuildingTypeTree = new Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.GH_Integer>();
            if ((!DA.GetDataTree(5, out comboBuildingTypeTree)))
                return;

            Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.GH_Number> shortestAngularDistTree = new Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.GH_Number>();
            if ((!DA.GetDataTree(6, out shortestAngularDistTree)))
                return;
            
            List<Rhino.Geometry.Surface> streetNetworkGenericDataTree = new List<Rhino.Geometry.Surface>();
            if ((!DA.GetDataList<Rhino.Geometry.Surface>(7, streetNetworkGenericDataTree)))
                return;

            int total_parcels = streetNetworkGenericDataTree.Count;
            int total_residents = ResidentNameList.Count;
            string strA = "A";
            int cluster_count = 0;
            for (var i = 0; i < inputClusterNameTagList.Count; i++)
            {
                string tmpstr = inputClusterNameTagList[i].ToString();
                int clusterid = Convert.ToChar(tmpstr) - Convert.ToChar(strA);
                if (clusterid >= cluster_count)
                    cluster_count = clusterid + 1;
            }
           
            // Setup the Cluster list array 
            List<ResidentNode>[] ClusterList = new List<ResidentNode>[cluster_count];
            for (int i = 0; i < cluster_count; i++)
                ClusterList[i] = new List<ResidentNode>();

            // Setup information of residents in the Cluster arry
            for (int ID = 0; ID < inputClusterNameTagList.Count; ID++)
            {
                ResidentNode node = new ResidentNode();
                node.ResidentID = ID;
                node.ParcelID = -1;
                node.originalArea = OriginalAreaList[ID];
                node.ClusterID = Convert.ToChar(inputClusterNameTagList[ID].ToString()) - Convert.ToChar(strA);
                ClusterList[node.ClusterID].Add(node);
            }

            // sort by Area
            for(int i = 0; i < cluster_count; i++)
                ClusterList[i].Sort(delegate (ResidentNode a_node1, ResidentNode a_node2) { return a_node1.originalArea.CompareTo(a_node2.originalArea); });

            // setup ClusterType priority 
            List<UnitInformation> tmpList = new List<UnitInformation>();
            for (int i = 0; i < cluster_count; i++)
            {
                UnitInformation tmp = new UnitInformation();
                tmp.Priority = i;
                tmp.index = i;
                tmpList.Add(tmp);
            }
            // generate all possible priorities
            IEnumerable<List<UnitInformation>> clusterTypePrioritylists_ie = factorialList(tmpList);
            
            // change to List
            List<List<UnitInformation>> clusterTypePrioritylists = new List<List<UnitInformation>>();
            foreach (List<UnitInformation> clusterTypePrioritylist_ie in clusterTypePrioritylists_ie)
                clusterTypePrioritylists.Add(clusterTypePrioritylist_ie);
            
            Grasshopper.Kernel.Data.GH_Path path;
            // setup ComboBuildingType priority 
            path = comboBuildingTypeTree.get_Path(0);
            //int maxComboNum = comboBuildingTypeTree.get_Branch(path).Count;

            tmpList.Clear();
            for (int i = 0; i < comboBuildingTypeTree.Branches.Count; i++)
            {
                UnitInformation tmp = new UnitInformation();
                tmp.Priority = i;
                tmp.index = i;
                tmpList.Add(tmp);
            }
            // generate all possible priorities
            IEnumerable<List<UnitInformation>> comboBuildingTypePrioritylists_ie = factorialList(tmpList);
            // change to List
            List<List<UnitInformation>> comboBuildingTypePrioritylists = new List<List<UnitInformation>>();
            foreach (List<UnitInformation> comboBuildingTypePrioritylist_ie in comboBuildingTypePrioritylists_ie)
                comboBuildingTypePrioritylists.Add(comboBuildingTypePrioritylist_ie);

            ComboType[] ComboTypeArry = new ComboType[comboBuildingTypeTree.Branches.Count];
            for (int i = 0; i < comboBuildingTypeTree.Branches.Count; i++)
            {
                ComboTypeArry[i] = new ComboType();
                ComboTypeArry[i].ComboTypeID = i;
                ComboTypeArry[i].bFull = false;
                ComboTypeArry[i].UnitCombolst = new List<UnitInformation>();

                path = comboBuildingTypeTree.get_Path(i);
                for (int j = 0; j < comboBuildingTypeTree.get_Branch(path).Count; j++)
                {
                    Grasshopper.Kernel.Types.GH_Integer number = comboBuildingTypeTree.get_DataItem(path, j);
                    UnitInformation unit = new UnitInformation();
                    unit.UnitTypeID = number.Value;
                    unit.UnitTypeAreaSize = UnitAreaSizeList[unit.UnitTypeID];
                    unit.index = j;
                    ComboTypeArry[i].UnitCombolst.Add(unit);
                }
                ComboTypeArry[i].UnitCombolst.Sort(delegate (UnitInformation a_node1, UnitInformation a_node2) { return a_node1.Priority.CompareTo(a_node2.Priority); });
            }
               
            // setup the AngularDistance list 2D arry
            int elem_count = shortestAngularDistTree.PathCount;
           
            List<ParcelNode>[] AngularDistList = new List<ParcelNode>[total_parcels];
            for (int j = 0; j < total_parcels; j++)
                AngularDistList[j] = new List<ParcelNode>();

            for (int j = 0; j < elem_count; j++)
            {
                path = shortestAngularDistTree.get_Path(j);
                int index_0 = path[0];
                int index_1 = path[1];

                ParcelNode Angle_node = new ParcelNode();
                Angle_node.ParcelID = index_1;
                Angle_node.ComboTypeID = -1;
                Grasshopper.Kernel.Types.GH_Number number = shortestAngularDistTree.get_DataItem(path, 0);
                Angle_node.angularDistance = number.Value;
                AngularDistList[index_0].Add(Angle_node);
            }

            for (int j=0; j < total_parcels; j++)
            {
                ParcelNode Angle_node = new ParcelNode();
                Angle_node.ParcelID = j;
                Angle_node.ComboTypeID = -1;
                Angle_node.angularDistance = (double)1.0;
                AngularDistList[j].Add(Angle_node);
            }

            // Sort the angular distances
            for (int j = 0; j < total_parcels; j++)
                AngularDistList[j].Sort(delegate (ParcelNode a_node1, ParcelNode a_node2) { return a_node2.angularDistance.CompareTo(a_node1.angularDistance); });
            
            //LinkedList<List<ParcelNode>>[,] choiceMap = new LinkedList<List<ParcelNode>>[comboBuildingTypePrioritylists.Count, clusterTypePrioritylists.Count];
            List<ParcelNode>[,,] choiceMap = new List<ParcelNode>[comboBuildingTypePrioritylists.Count, clusterTypePrioritylists.Count, total_parcels];
            for (int i = 0; i < comboBuildingTypePrioritylists.Count; i++)
            {
                int count = 0;
                ComboType[] newComboTypeArry = new ComboType[comboBuildingTypeTree.Branches.Count];
                foreach (UnitInformation unit in comboBuildingTypePrioritylists[i])
                {
                    newComboTypeArry[count] = ComboTypeArry[unit.index];
                    newComboTypeArry[count].UnitCombolst = new List<UnitInformation>(ComboTypeArry[unit.index].UnitCombolst.ToArray());
                    count++;
                }

                for (int j = 0; j < clusterTypePrioritylists.Count; j++)
                {
                    for (int k = 0; k < total_parcels; k++)
                    {
                        choiceMap[i, j, k] = new List<ParcelNode>();
                        count = 0;
                        foreach (ParcelNode Angle_node in AngularDistList[k])
                        {
                            ParcelNode choice_node = new ParcelNode();

                            choice_node.ParcelID = Angle_node.ParcelID;
                            choice_node.ComboTypeID = Angle_node.ComboTypeID;
                            choice_node.angularDistance = Angle_node.angularDistance;
                            choice_node.ComboTypeID = Angle_node.ComboTypeID;
                            choice_node.bFull = Angle_node.bFull;
                            choiceMap[i, j, k].Add(choice_node);
                        }
                    }

                    List<ResidentNode>[] newClusterList = new List<ResidentNode>[ClusterList.Length];
                    count = 0;
                    foreach (UnitInformation unit in clusterTypePrioritylists[j])
                    {
                        newClusterList[count] = new List<ResidentNode>(ClusterList[unit.index]);
                        count++;
                    }

                    foreach (List<ResidentNode> cluster in newClusterList)
                    {
                        foreach (ResidentNode clusternode in cluster)
                        {
                            for (int k = 0; k < total_parcels; k++)
                            {
                                bool bfound = false;
                                List<ParcelNode> choice_parcel_node_list = choiceMap[i, j, k];
                                foreach (ParcelNode choice_parcel_node in choice_parcel_node_list)
                                {
                                    if (choice_parcel_node.bFull == false)
                                    {
                                        if (bfound == false)
                                        {
                                            bfound = choice_parcel_node.FindSuitableComboType(clusternode, newComboTypeArry);

                                        }
                                    }
                                }
                            }
                        }
                        for (int k = 0; k < total_parcels; k++)
                        {
                            List<ParcelNode> choice_parcel_node_list = choiceMap[i, j, k];
                            count = 0;
                            foreach (ParcelNode choice_parcel_node in choice_parcel_node_list)
                            {   
                                if ((choice_parcel_node.bFull == false)&&(choice_parcel_node.ComboTypeID >= 0))
                                {
                                    bool bFull = true;
                                    foreach (UnitInformation unit in choice_parcel_node.UnitCombolst)
                                        bFull = unit.bOccupied & bFull;
                                    if (bFull)
                                        choiceMap[i, j, k][count].bFull = true;

                                }
                                count++;
                            }
                        }
                        for (int k = 0; k < total_parcels; k++)
                        {
                            int counter = 0;
                            List<ParcelNode> choice_parcel_node_list = choiceMap[i, j, k];
                            while (choice_parcel_node_list[counter].bFull)
                                counter++;

                            if (counter>0)
                            {
                                choiceMap[i, j, k].Sort(delegate (ParcelNode a_node1, ParcelNode a_node2) { return a_node1.ParcelID.CompareTo(a_node2.ParcelID); });
                                List<ParcelNode> AngularDist = AngularDistList[counter];
                                AngularDist.Sort(delegate (ParcelNode a_node1, ParcelNode a_node2) { return a_node1.ParcelID.CompareTo(a_node2.ParcelID); });
                                for (int tmp = 0; tmp < choice_parcel_node_list.Count; tmp++)
                                    choiceMap[i, j, k][tmp].angularDistance = AngularDist[tmp].angularDistance;
                                choiceMap[i, j, k].Sort(delegate (ParcelNode a_node1, ParcelNode a_node2) { return a_node2.angularDistance.CompareTo(a_node1.angularDistance); });
                            }
                        }
                        for (int k = 0; k < total_parcels; k++)
                            AngularDistList[k].Sort(delegate (ParcelNode a_node1, ParcelNode a_node2) { return a_node2.angularDistance.CompareTo(a_node1.angularDistance); });

                    }
                }
            }
            
            // keep the locations
            List < Point3d >[,,,] pt_List = new List<Point3d>[comboBuildingTypePrioritylists.Count, clusterTypePrioritylists.Count, total_parcels, cluster_count];
            for ( int i = 0; i < comboBuildingTypePrioritylists.Count; i++)
                for (int l = 0; l < clusterTypePrioritylists.Count; l++)
                    for (int j = 0; j < total_parcels; j++)
                        for (int k = 0; k < cluster_count; k++)
                            pt_List[i, l, j, k] = new List<Point3d>();

            List <ParcelNode>[,] dist_Lists = new List<ParcelNode>[comboBuildingTypePrioritylists.Count, clusterTypePrioritylists.Count];
            for (int i = 0; i < comboBuildingTypePrioritylists.Count; i++)
                for (int k = 0; k < clusterTypePrioritylists.Count; k++)
                {
                    dist_Lists[i,k] = new List<ParcelNode>();
                    for (int j = 0; j < total_parcels; j++)
                    {
                        List<ParcelNode> Angle_node_list = choiceMap[i, k, j];
                        foreach (ParcelNode Angle_node in Angle_node_list)
                        {
                            Point3d centerpt;
                            Vector3d[] centervt;
                            int tmp = Angle_node.ParcelID;
                            if (Angle_node.ComboTypeID >= 0)
                            {
                                streetNetworkGenericDataTree[tmp].Evaluate(0.5, 0.5, 1, out centerpt, out centervt);
                                foreach (UnitInformation unit in Angle_node.UnitCombolst)
                                {
                                    if (unit.ClusterID >= 0)
                                        pt_List[i, k, j, unit.ClusterID].Add(centerpt);
                                }
                            }
                        }
                        ParcelNode pnode = new ParcelNode();
                        pnode.angularDistance = 0;
                        pnode.ParcelID = j;
                        dist_Lists[i, k].Add(pnode);
                    }
                }

            for (int i = 0; i < comboBuildingTypePrioritylists.Count; i++)
                for (int l = 0; l < clusterTypePrioritylists.Count; l++)
                {
                    List<ParcelNode> dist_List = dist_Lists[i, l];
                    for (int j = 0; j < total_parcels; j++)
                    {
                        for (int k = 0; k < cluster_count; k++)
                        {
                            Rhino.Geometry.BoundingBox box = new Rhino.Geometry.BoundingBox(pt_List[i, l, j, k]);
                            Rhino.Geometry.Vector3d vt = box.Diagonal;
                            dist_List[j].angularDistance += vt.Length;
                        }
                    }
                }

            List<ParcelNode>[] best_Lists = new List<ParcelNode>[comboBuildingTypePrioritylists.Count];
            for (int i = 0; i < comboBuildingTypePrioritylists.Count; i++)
                best_Lists[i] = new List<ParcelNode>();

            for (int i = 0; i < comboBuildingTypePrioritylists.Count; i++)
                for (int l = 0; l < clusterTypePrioritylists.Count; l++)
                {
                    dist_Lists[i,l].Sort(delegate (ParcelNode a_node1, ParcelNode a_node2) { return a_node1.angularDistance.CompareTo(a_node2.angularDistance); });
                    ParcelNode tmp= new ParcelNode();
                    tmp.ParcelID = dist_Lists[i,l][0].ParcelID;
                    tmp.angularDistance = dist_Lists[i,l][0].angularDistance;
                    tmp.ComboTypeID = i; // temporially use to keep the index of ComboTypeLists
                    tmp.ClusterID = l;
                    best_Lists[i].Add(tmp);
                }

            for (int i = 0; i < comboBuildingTypePrioritylists.Count; i++)
                best_Lists[i].Sort(delegate (ParcelNode a_node1, ParcelNode a_node2) { return a_node1.angularDistance.CompareTo(a_node2.angularDistance); });

            List<ParcelNode> best_Lists_all = new List<ParcelNode>();
            for (int i = 0; i < comboBuildingTypePrioritylists.Count; i++)
            {
                ParcelNode tmp = new ParcelNode();
                tmp.ParcelID = best_Lists[i][0].ParcelID;
                tmp.angularDistance = best_Lists[i][0].angularDistance;
                tmp.ClusterID = best_Lists[i][0].ClusterID;
                tmp.ComboTypeID = i; // temporially use to keep the index of ComboTypeLists
                best_Lists_all.Add(tmp);
            }

            best_Lists_all.Sort(delegate (ParcelNode a_node1, ParcelNode a_node2) { return a_node1.angularDistance.CompareTo(a_node2.angularDistance); });

            int best_index = best_Lists_all[0].ParcelID;
            int best_index_ComboTypeList = best_Lists_all[0].ComboTypeID;
            int best_index_ClusterList = best_Lists_all[0].ClusterID;

            Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.GH_String> outputClusterNameTagList = new Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.GH_String>();
            Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.GH_String> outputResidentNameList = new Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.GH_String>();
            List<int> outputComboBuildingTypeList = new List<int>();

            List<ParcelNode> best = choiceMap[best_index_ComboTypeList, best_index_ClusterList, best_index];
            int index = 0;
            best.Sort(delegate (ParcelNode a_node1, ParcelNode a_node2) { return a_node1.ParcelID.CompareTo(a_node2.ParcelID); });
            foreach (var parcel in best)
            {
                Grasshopper.Kernel.Data.GH_Path newpath = new Grasshopper.Kernel.Data.GH_Path(index);
                if (parcel.ComboTypeID >= 0)
                {
                    parcel.UnitCombolst.Sort(delegate (UnitInformation a_node1, UnitInformation a_node2) { return a_node1.index.CompareTo(a_node2.index); });
                    foreach (UnitInformation unit in parcel.UnitCombolst)
                    {
                        if (unit.bOccupied)
                        {
                            Grasshopper.Kernel.Types.GH_String tmp1 = new Grasshopper.Kernel.Types.GH_String(inputClusterNameTagList[unit.ResidentID]);
                            outputClusterNameTagList.Append(tmp1, newpath);
                            Grasshopper.Kernel.Types.GH_String tmp2 = new Grasshopper.Kernel.Types.GH_String(ResidentNameList[unit.ResidentID]);
                            outputResidentNameList.Append(tmp2, newpath);
                        }
                        else
                        {
                            Grasshopper.Kernel.Types.GH_String tmp1 = new Grasshopper.Kernel.Types.GH_String("");
                            outputClusterNameTagList.Append(tmp1, newpath);
                            Grasshopper.Kernel.Types.GH_String tmp2 = new Grasshopper.Kernel.Types.GH_String("");
                            outputResidentNameList.Append(tmp2, newpath);
                        }
                    }
                }
                else
                {
                    Grasshopper.Kernel.Types.GH_String tmp1 = new Grasshopper.Kernel.Types.GH_String("");
                    outputClusterNameTagList.Append(tmp1, newpath);
                    Grasshopper.Kernel.Types.GH_String tmp2 = new Grasshopper.Kernel.Types.GH_String("");
                    outputResidentNameList.Append(tmp2, newpath);
                }

                outputComboBuildingTypeList.Add(parcel.ComboTypeID);
                index++;
            }   
            
            DA.SetDataTree(0, outputClusterNameTagList);
            DA.SetDataTree(1, outputResidentNameList);
            DA.SetDataList(2, outputComboBuildingTypeList);
        }
                

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                return Clustering.Properties.Resources.cluster;
                //return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{82d15b92-ac01-4ff8-b63a-e7804118fdad}"); }
        }

        protected IEnumerable<List<UnitInformation>> factorialList(List<UnitInformation> input)
        {
            if (input.Count == 2)
            {
                yield return new List<UnitInformation>(input);
                yield return new List<UnitInformation> { input[1], input[0] };
            }
            else
            {
                foreach(UnitInformation elem in input)
                {
                    var rlist = new List<UnitInformation>(input);
                    rlist.Remove(elem);
                    foreach(List<UnitInformation> retlist in factorialList(rlist))
                    {
                        retlist.Insert(0, elem);
                        yield return retlist;
                    }
                }
            }
        }

    }
}

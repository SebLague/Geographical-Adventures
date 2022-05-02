// -----------------------------------------------------------------------
// <copyright file="CuthillMcKee.cs" company="">
// Original Matlab code by John Burkardt, Florida State University
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Tools
{
    using System;

    /// <summary>
    /// Applies the Cuthill and McKee renumbering algorithm to reduce the bandwidth of
    /// the adjacency matrix associated with the mesh.
    /// </summary>
    public class CuthillMcKee
    {
        // The adjacency matrix of the mesh.
        AdjacencyMatrix matrix;

        /// <summary>
        /// Gets the permutation vector for the Reverse Cuthill-McKee numbering.
        /// </summary>
        /// <param name="mesh">The mesh.</param>
        /// <returns>Permutation vector.</returns>
        public int[] Renumber(Mesh mesh)
        {
            // Algorithm needs linear numbering of the nodes.
            mesh.Renumber(NodeNumbering.Linear);

            return Renumber(new AdjacencyMatrix(mesh));
        }

        /// <summary>
        /// Gets the permutation vector for the Reverse Cuthill-McKee numbering.
        /// </summary>
        /// <param name="mesh">The mesh.</param>
        /// <returns>Permutation vector.</returns>
        public int[] Renumber(AdjacencyMatrix matrix)
        {
            this.matrix = matrix;

            int bandwidth1 = matrix.Bandwidth();

            var pcol = matrix.ColumnPointers;

            // Adjust column pointers (1-based indexing).
            Shift(pcol, true);

            // TODO: Make RCM work with 0-based matrix.

            // Compute the RCM permutation.
            int[] perm = GenerateRcm();

            int[] perm_inv = PermInverse(perm);

            int bandwidth2 = PermBandwidth(perm, perm_inv);

            if (Log.Verbose)
            {
                Log.Instance.Info(String.Format("Reverse Cuthill-McKee (Bandwidth: {0} > {1})",
                    bandwidth1, bandwidth2));
            }

            // Adjust column pointers (0-based indexing).
            Shift(pcol, false);

            return perm_inv;
        }

        #region RCM

        /// <summary>
        /// Finds the reverse Cuthill-Mckee ordering for a general graph.
        /// </summary>
        /// <returns>The RCM ordering.</returns>
        /// <remarks>
        /// For each connected component in the graph, the routine obtains
        /// an ordering by calling RCM.
        /// </remarks>
        int[] GenerateRcm()
        {
            // Number of nodes in the mesh.
            int n = matrix.N;

            int[] perm = new int[n];

            int i, num, root;
            int iccsze = 0;
            int level_num = 0;

            /// Index vector for a level structure. The level structure is stored in the
            /// currently unused  spaces in the permutation vector PERM.
            int[] level_row = new int[n + 1];

            /// Marks variables that have been numbered.
            int[] mask = new int[n];

            for (i = 0; i < n; i++)
            {
                mask[i] = 1;
            }

            num = 1;

            for (i = 0; i < n; i++)
            {
                // For each masked connected component...
                if (mask[i] != 0)
                {
                    root = i;

                    // Find a pseudo-peripheral node ROOT. The level structure found by
                    // ROOT_FIND is stored starting at PERM(NUM).
                    FindRoot(ref root, mask, ref level_num, level_row, perm, num - 1);

                    // RCM orders the component using ROOT as the starting node.
                    Rcm(root, mask, perm, num - 1, ref iccsze);

                    num += iccsze;

                    // We can stop once every node is in one of the connected components.
                    if (n < num)
                    {
                        return perm;
                    }
                }
            }

            return perm;
        }

        /// <summary>
        /// RCM renumbers a connected component by the reverse Cuthill McKee algorithm.
        /// </summary>
        /// <param name="root">the node that defines the connected component. It is used as the starting 
        /// point for the RCM ordering.</param>
        /// <param name="mask">Input/output, int MASK(NODE_NUM), a mask for the nodes. Only those nodes with 
        /// nonzero input mask values are considered by the routine. The nodes numbered by RCM will have 
        /// their mask values set to zero.</param>
        /// <param name="perm">Output, int PERM(NODE_NUM), the RCM ordering.</param>
        /// <param name="iccsze">Output, int ICCSZE, the size of the connected component that has been numbered.</param>
        /// <param name="node_num">the number of nodes.</param>
        /// <remarks>
        ///    The connected component is specified by a node ROOT and a mask.
        ///    The numbering starts at the root node.
        ///
        ///    An outline of the algorithm is as follows:
        ///
        ///    X(1) = ROOT.
        ///
        ///    for ( I = 1 to N-1)
        ///      Find all unlabeled neighbors of X(I),
        ///      assign them the next available labels, in order of increasing degree.
        ///
        ///    When done, reverse the ordering.
        /// </remarks>
        void Rcm(int root, int[] mask, int[] perm, int offset, ref int iccsze)
        {
            int[] pcol = matrix.ColumnPointers;
            int[] irow = matrix.RowIndices;

            int fnbr;
            int i, j, k, l;
            int jstop, jstrt;
            int lbegin, lnbr, lperm, lvlend;
            int nbr, node;

            // Number of nodes in the mesh.
            int n = matrix.N;

            /// Workspace, int DEG[NODE_NUM], a temporary vector used to hold 
            /// the degree of the nodes in the section graph specified by mask and root.
            int[] deg = new int[n];

            // Find the degrees of the nodes in the component specified by MASK and ROOT.
            Degree(root, mask, deg, ref iccsze, perm, offset);

            mask[root] = 0;

            if (iccsze <= 1)
            {
                return;
            }

            lvlend = 0;
            lnbr = 1;

            // LBEGIN and LVLEND point to the beginning and
            // the end of the current level respectively.
            while (lvlend < lnbr)
            {
                lbegin = lvlend + 1;
                lvlend = lnbr;

                for (i = lbegin; i <= lvlend; i++)
                {
                    // For each node in the current level...
                    node = perm[offset + i - 1];
                    jstrt = pcol[node];
                    jstop = pcol[node + 1] - 1;

                    // Find the unnumbered neighbors of NODE.

                    // FNBR and LNBR point to the first and last neighbors
                    // of the current node in PERM.
                    fnbr = lnbr + 1;

                    for (j = jstrt; j <= jstop; j++)
                    {
                        nbr = irow[j - 1];

                        if (mask[nbr] != 0)
                        {
                            lnbr += 1;
                            mask[nbr] = 0;
                            perm[offset + lnbr - 1] = nbr;
                        }
                    }

                    // Node has neighbors
                    if (lnbr > fnbr)
                    {
                        // Sort the neighbors of NODE in increasing order by degree.
                        // Linear insertion is used.
                        k = fnbr;

                        while (k < lnbr)
                        {
                            l = k;
                            k = k + 1;
                            nbr = perm[offset + k - 1];

                            while (fnbr < l)
                            {
                                lperm = perm[offset + l - 1];

                                if (deg[lperm - 1] <= deg[nbr - 1])
                                {
                                    break;
                                }

                                perm[offset + l] = lperm;
                                l = l - 1;
                            }
                            perm[offset + l] = nbr;
                        }
                    }
                }
            }

            // We now have the Cuthill-McKee ordering. Reverse it.
            ReverseVector(perm, offset, iccsze);

            return;
        }

        /// <summary>
        /// Finds a pseudo-peripheral node.
        /// </summary>
        /// <param name="root">On input, ROOT is a node in the the component of the graph for 
        /// which a pseudo-peripheral node is sought. On output, ROOT is the pseudo-peripheral 
        /// node obtained.</param>
        /// <param name="mask">MASK[NODE_NUM], specifies a section subgraph. Nodes for which MASK 
        /// is zero are ignored by FNROOT.</param>
        /// <param name="level_num">Output, int LEVEL_NUM, is the number of levels in the level 
        /// structure rooted at the node ROOT.</param>
        /// <param name="level_row">Output, int LEVEL_ROW(NODE_NUM+1), the level structure array pair 
        /// containing the level structure found.</param>
        /// <param name="level">Output, int LEVEL(NODE_NUM), the level structure array pair 
        /// containing the level structure found.</param>
        /// <param name="node_num">the number of nodes.</param>
        /// <remarks>
        /// The diameter of a graph is the maximum distance (number of edges)
        /// between any two nodes of the graph.
        ///
        /// The eccentricity of a node is the maximum distance between that
        /// node and any other node of the graph.
        ///
        /// A peripheral node is a node whose eccentricity equals the
        /// diameter of the graph.
        ///
        /// A pseudo-peripheral node is an approximation to a peripheral node;
        /// it may be a peripheral node, but all we know is that we tried our
        /// best.
        ///
        /// The routine is given a graph, and seeks pseudo-peripheral nodes,
        /// using a modified version of the scheme of Gibbs, Poole and
        /// Stockmeyer.  It determines such a node for the section subgraph
        /// specified by MASK and ROOT.
        ///
        /// The routine also determines the level structure associated with
        /// the given pseudo-peripheral node; that is, how far each node
        /// is from the pseudo-peripheral node.  The level structure is
        /// returned as a list of nodes LS, and pointers to the beginning
        /// of the list of nodes that are at a distance of 0, 1, 2, ...,
        /// NODE_NUM-1 from the pseudo-peripheral node.
        ///
        /// Reference:
        ///    Alan George, Joseph Liu,
        ///    Computer Solution of Large Sparse Positive Definite Systems,
        ///    Prentice Hall, 1981.
        ///
        ///    Norman Gibbs, William Poole, Paul Stockmeyer,
        ///    An Algorithm for Reducing the Bandwidth and Profile of a Sparse Matrix,
        ///    SIAM Journal on Numerical Analysis,
        ///    Volume 13, pages 236-250, 1976.
        ///
        ///    Norman Gibbs,
        ///    Algorithm 509: A Hybrid Profile Reduction Algorithm,
        ///    ACM Transactions on Mathematical Software,
        ///    Volume 2, pages 378-387, 1976.
        /// </remarks>
        void FindRoot(ref int root, int[] mask, ref int level_num, int[] level_row,
            int[] level, int offset)
        {
            int[] pcol = matrix.ColumnPointers;
            int[] irow = matrix.RowIndices;

            int iccsze;
            int j, jstrt;
            int k, kstop, kstrt;
            int mindeg;
            int nghbor, ndeg;
            int node;
            int level_num2 = 0;

            // Determine the level structure rooted at ROOT.
            GetLevelSet(ref root, mask, ref level_num, level_row, level, offset);

            // Count the number of nodes in this level structure.
            iccsze = level_row[level_num] - 1;

            // Extreme cases:
            //   A complete graph has a level set of only a single level.
            //   Every node is equally good (or bad).
            // or
            //   A "line graph" 0--0--0--0--0 has every node in its only level.
            //   By chance, we've stumbled on the ideal root.
            if (level_num == 1 || level_num == iccsze)
            {
                return;
            }

            // Pick any node from the last level that has minimum degree
            // as the starting point to generate a new level set.
            for (; ; )
            {
                mindeg = iccsze;

                jstrt = level_row[level_num - 1];
                root = level[offset + jstrt - 1];

                if (jstrt < iccsze)
                {
                    for (j = jstrt; j <= iccsze; j++)
                    {
                        node = level[offset + j - 1];
                        ndeg = 0;
                        kstrt = pcol[node - 1];
                        kstop = pcol[node] - 1;

                        for (k = kstrt; k <= kstop; k++)
                        {
                            nghbor = irow[k - 1];
                            if (mask[nghbor] > 0)
                            {
                                ndeg += 1;
                            }
                        }

                        if (ndeg < mindeg)
                        {
                            root = node;
                            mindeg = ndeg;
                        }
                    }
                }

                // Generate the rooted level structure associated with this node.
                GetLevelSet(ref root, mask, ref level_num2, level_row, level, offset);

                // If the number of levels did not increase, accept the new ROOT.
                if (level_num2 <= level_num)
                {
                    break;
                }

                level_num = level_num2;

                // In the unlikely case that ROOT is one endpoint of a line graph,
                // we can exit now.
                if (iccsze <= level_num)
                {
                    break;
                }
            }

            return;
        }

        /// <summary>
        /// Generates the connected level structure rooted at a given node.
        /// </summary>
        /// <param name="root">the node at which the level structure is to be rooted.</param>
        /// <param name="mask">MASK[NODE_NUM]. On input, only nodes with nonzero MASK are to be processed. 
        /// On output, those nodes which were included in the level set have MASK set to 1.</param>
        /// <param name="level_num">Output, int LEVEL_NUM, the number of levels in the level structure. ROOT is 
        /// in level 1.  The neighbors of ROOT are in level 2, and so on.</param>
        /// <param name="level_row">Output, int LEVEL_ROW[NODE_NUM+1], the rooted level structure.</param>
        /// <param name="level">Output, int LEVEL[NODE_NUM], the rooted level structure.</param>
        /// <param name="node_num">the number of nodes.</param>
        /// <remarks>
        /// Only nodes for which MASK is nonzero will be considered.
        ///
        /// The root node chosen by the user is assigned level 1, and masked.
        /// All (unmasked) nodes reachable from a node in level 1 are
        /// assigned level 2 and masked.  The process continues until there
        /// are no unmasked nodes adjacent to any node in the current level.
        /// The number of levels may vary between 2 and NODE_NUM.
        ///
        /// Reference:
        ///    Alan George, Joseph Liu,
        ///    Computer Solution of Large Sparse Positive Definite Systems,
        ///    Prentice Hall, 1981.
        /// </remarks>
        void GetLevelSet(ref int root, int[] mask, ref int level_num, int[] level_row,
            int[] level, int offset)
        {
            int[] pcol = matrix.ColumnPointers;
            int[] irow = matrix.RowIndices;

            int i, iccsze;
            int j, jstop, jstrt;
            int lbegin, lvlend, lvsize;
            int nbr;
            int node;

            mask[root] = 0;
            level[offset] = root;
            level_num = 0;
            lvlend = 0;
            iccsze = 1;

            // LBEGIN is the pointer to the beginning of the current level, and
            // LVLEND points to the end of this level.
            for (; ; )
            {
                lbegin = lvlend + 1;
                lvlend = iccsze;
                level_num += 1;
                level_row[level_num - 1] = lbegin;

                // Generate the next level by finding all the masked neighbors of nodes
                // in the current level.
                for (i = lbegin; i <= lvlend; i++)
                {
                    node = level[offset + i - 1];
                    jstrt = pcol[node];
                    jstop = pcol[node + 1] - 1;

                    for (j = jstrt; j <= jstop; j++)
                    {
                        nbr = irow[j - 1];

                        if (mask[nbr] != 0)
                        {
                            iccsze += 1;
                            level[offset + iccsze - 1] = nbr;
                            mask[nbr] = 0;
                        }
                    }
                }

                // Compute the current level width (the number of nodes encountered.)
                // If it is positive, generate the next level.
                lvsize = iccsze - lvlend;

                if (lvsize <= 0)
                {
                    break;
                }
            }

            level_row[level_num] = lvlend + 1;

            // Reset MASK to 1 for the nodes in the level structure.
            for (i = 0; i < iccsze; i++)
            {
                mask[level[offset + i]] = 1;
            }

            return;
        }

        /// <summary>
        /// Computes the degrees of the nodes in the connected component.
        /// </summary>
        /// <param name="root">the node that defines the connected component.</param>
        /// <param name="mask">MASK[NODE_NUM], is nonzero for those nodes which are to be considered.</param>
        /// <param name="deg">Output, int DEG[NODE_NUM], contains, for each  node in the connected component, its degree.</param>
        /// <param name="iccsze">Output, int ICCSIZE, the number of nodes in the connected component.</param>
        /// <param name="ls">Output, int LS[NODE_NUM], stores in entries 1 through ICCSIZE the nodes in the 
        /// connected component, starting with ROOT, and proceeding by levels.</param>
        /// <param name="node_num">the number of nodes.</param>
        /// <remarks>
        ///    The connected component is specified by MASK and ROOT.
        ///    Nodes for which MASK is zero are ignored.
        ///
        ///  Reference:
        ///    Alan George, Joseph Liu,
        ///    Computer Solution of Large Sparse Positive Definite Systems,
        ///    Prentice Hall, 1981.
        /// </remarks>
        void Degree(int root, int[] mask, int[] deg, ref int iccsze, int[] ls, int offset)
        {
            int[] pcol = matrix.ColumnPointers;
            int[] irow = matrix.RowIndices;

            int i, ideg;
            int j, jstop, jstrt;
            int lbegin, lvlend;
            int lvsize = 1;
            int nbr, node;

            // The sign of ADJ_ROW(I) is used to indicate if node I has been considered.
            ls[offset] = root;
            pcol[root] = -pcol[root];
            lvlend = 0;
            iccsze = 1;

            // If the current level width is nonzero, generate another level.
            while (lvsize > 0)
            {
                // LBEGIN is the pointer to the beginning of the current level, and
                // LVLEND points to the end of this level.
                lbegin = lvlend + 1;
                lvlend = iccsze;

                // Find the degrees of nodes in the current level,
                // and at the same time, generate the next level.
                for (i = lbegin; i <= lvlend; i++)
                {
                    node = ls[offset + i - 1];
                    jstrt = -pcol[node];
                    jstop = Math.Abs(pcol[node + 1]) - 1;
                    ideg = 0;

                    for (j = jstrt; j <= jstop; j++)
                    {
                        nbr = irow[j - 1];

                        if (mask[nbr] != 0) // EDIT: [nbr - 1]
                        {
                            ideg = ideg + 1;

                            if (0 <= pcol[nbr]) // EDIT: [nbr - 1]
                            {
                                pcol[nbr] = -pcol[nbr]; // EDIT: [nbr - 1]
                                iccsze = iccsze + 1;
                                ls[offset + iccsze - 1] = nbr;
                            }
                        }
                    }
                    deg[node] = ideg;
                }

                // Compute the current level width.
                lvsize = iccsze - lvlend;
            }

            // Reset ADJ_ROW to its correct sign and return.
            for (i = 0; i < iccsze; i++)
            {
                node = ls[offset + i];
                pcol[node] = -pcol[node];
            }

            return;
        }

        #endregion

        #region Tools

        /// <summary>
        /// Computes the bandwidth of a permuted adjacency matrix.
        /// </summary>
        /// <param name="perm">The permutation.</param>
        /// <param name="perm_inv">The inverse permutation.</param>
        /// <returns>Bandwidth of the permuted adjacency matrix.</returns>
        /// <remarks>
        /// The matrix is defined by the adjacency information and a permutation.  
        /// The routine also computes the bandwidth and the size of the envelope.
        /// </remarks>
        int PermBandwidth(int[] perm, int[] perm_inv)
        {
            int[] pcol = matrix.ColumnPointers;
            int[] irow = matrix.RowIndices;

            int col, i, j;

            int band_lo = 0;
            int band_hi = 0;

            int n = matrix.N;

            for (i = 0; i < n; i++)
            {
                for (j = pcol[perm[i]]; j < pcol[perm[i] + 1]; j++)
                {
                    col = perm_inv[irow[j - 1]];
                    band_lo = Math.Max(band_lo, i - col);
                    band_hi = Math.Max(band_hi, col - i);
                }
            }

            return band_lo + 1 + band_hi;
        }

        /// <summary>
        /// Produces the inverse of a given permutation.
        /// </summary>
        /// <param name="n">Number of items permuted.</param>
        /// <param name="perm">PERM[N], a permutation.</param>
        /// <returns>The inverse permutation.</returns>
        int[] PermInverse(int[] perm)
        {
            int n = matrix.N;

            int[] perm_inv = new int[n];

            for (int i = 0; i < n; i++)
            {
                perm_inv[perm[i]] = i;
            }

            return perm_inv;
        }

        /// <summary>
        /// Reverses the elements of an integer vector.
        /// </summary>
        /// <param name="size">number of entries in the array.</param>
        /// <param name="a">the array to be reversed.</param>
        /// <example>
        ///   Input:
        ///     N = 5,
        ///     A = ( 11, 12, 13, 14, 15 ).
        ///
        ///   Output:
        ///     A = ( 15, 14, 13, 12, 11 ).
        /// </example>
        void ReverseVector(int[] a, int offset, int size)
        {
            int i;
            int j;

            for (i = 0; i < size / 2; i++)
            {
                j = a[offset + i];
                a[offset + i] = a[offset + size - 1 - i];
                a[offset + size - 1 - i] = j;
            }

            return;
        }

        void Shift(int[] a, bool up)
        {
            int length = a.Length;

            if (up)
            {
                for (int i = 0; i < length; a[i]++, i++) ;
            }
            else
            {
                for (int i = 0; i < length; a[i]--, i++) ;
            }
        }

        #endregion
    }
}

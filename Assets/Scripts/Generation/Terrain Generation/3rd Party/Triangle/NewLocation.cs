// -----------------------------------------------------------------------
// <copyright file="NewLocation.cs">
// Original code by Hale Erten and Alper Üngör, http://www.cise.ufl.edu/~ungor/aCute/index.html
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet
{
    using System;
    using TriangleNet.Topology;
    using TriangleNet.Geometry;
    using TriangleNet.Tools;

    /// <summary>
    /// Find new Steiner point locations.
    /// </summary>
    /// <remarks>
    /// http://www.cise.ufl.edu/~ungor/aCute/index.html
    /// </remarks>
    class NewLocation
    {
        const double EPS = 1e-50;

        IPredicates predicates;

        Mesh mesh;
        Behavior behavior;

        // Work arrays for wegde intersection
        double[] petalx = new double[20];
        double[] petaly = new double[20];
        double[] petalr = new double[20];
        double[] wedges = new double[500];
        double[] initialConvexPoly = new double[500];

        // Work arrays for smoothing
        double[] points_p = new double[500];
        double[] points_q = new double[500];
        double[] points_r = new double[500];

        // Work arrays for convex polygon split
        double[] poly1 = new double[100];
        double[] poly2 = new double[100];
        double[][] polys = new double[3][];

        public NewLocation(Mesh mesh, IPredicates predicates)
        {
            this.mesh = mesh;
            this.predicates = predicates;

            this.behavior = mesh.behavior;
        }

        /// <summary>
        /// Find a new location for a Steiner point.
        /// </summary>
        /// <param name="org"></param>
        /// <param name="dest"></param>
        /// <param name="apex"></param>
        /// <param name="xi"></param>
        /// <param name="eta"></param>
        /// <param name="offcenter"></param>
        /// <param name="badotri"></param>
        /// <returns></returns>
        public Point FindLocation(Vertex org, Vertex dest, Vertex apex,
            ref double xi, ref double eta, bool offcenter, Otri badotri)
        {
            // Based on using -U switch, call the corresponding function
            if (behavior.MaxAngle == 0.0)
            {
                // Disable the "no max angle" code. It may return weired vertex locations.
                return FindNewLocationWithoutMaxAngle(org, dest, apex, ref xi, ref eta, true, badotri);
            }

            // With max angle
            return FindNewLocation(org, dest, apex, ref xi, ref eta, true, badotri);
        }

        /// <summary>
        /// Find a new location for a Steiner point.
        /// </summary>
        /// <param name="torg"></param>
        /// <param name="tdest"></param>
        /// <param name="tapex"></param>
        /// <param name="circumcenter"></param>
        /// <param name="xi"></param>
        /// <param name="eta"></param>
        /// <param name="offcenter"></param>
        /// <param name="badotri"></param>
        private Point FindNewLocationWithoutMaxAngle(Vertex torg, Vertex tdest, Vertex tapex,
            ref double xi, ref double eta, bool offcenter, Otri badotri)
        {
            double offconstant = behavior.offconstant;

            // for calculating the distances of the edges
            double xdo, ydo, xao, yao, xda, yda;
            double dodist, aodist, dadist;
            // for exact calculation
            double denominator;
            double dx, dy, dxoff, dyoff;

            ////////////////////////////// HALE'S VARIABLES //////////////////////////////
            // keeps the difference of coordinates edge 
            double xShortestEdge = 0, yShortestEdge = 0;

            // keeps the square of edge lengths
            double shortestEdgeDist = 0, middleEdgeDist = 0, longestEdgeDist = 0;

            // keeps the vertices according to the angle incident to that vertex in a triangle
            Point smallestAngleCorner, middleAngleCorner, largestAngleCorner;

            // keeps the type of orientation if the triangle
            int orientation = 0;
            // keeps the coordinates of circumcenter of itself and neighbor triangle circumcenter	
            Point myCircumcenter, neighborCircumcenter;

            // keeps if bad triangle is almost good or not
            int almostGood = 0;
            // keeps the cosine of the largest angle
            double cosMaxAngle;
            bool isObtuse; // 1: obtuse 0: nonobtuse
            // keeps the radius of petal
            double petalRadius;
            // for calculating petal center
            double xPetalCtr_1, yPetalCtr_1, xPetalCtr_2, yPetalCtr_2, xPetalCtr, yPetalCtr, xMidOfShortestEdge, yMidOfShortestEdge;
            double dxcenter1, dycenter1, dxcenter2, dycenter2;
            // for finding neighbor
            Otri neighborotri = default(Otri);
            double[] thirdPoint = new double[2];
            //int neighborNotFound = -1;
            bool neighborNotFound;
            // for keeping the vertices of the neighbor triangle
            Vertex neighborvertex_1;
            Vertex neighborvertex_2;
            Vertex neighborvertex_3;
            // dummy variables 
            double xi_tmp = 0, eta_tmp = 0;
            //vertex thirdVertex;
            // for petal intersection
            double vector_x, vector_y, xMidOfLongestEdge, yMidOfLongestEdge, inter_x, inter_y;
            double[] p = new double[5], voronoiOrInter = new double[4];
            bool isCorrect;

            // for vector calculations in perturbation
            double ax, ay, d;
            double pertConst = 0.06; // perturbation constant

            double lengthConst = 1; // used at comparing circumcenter's distance to proposed point's distance
            double justAcute = 1; // used for making the program working for one direction only
            // for smoothing
            int relocated = 0;// used to differentiate between calling the deletevertex and just proposing a steiner point
            double[] newloc = new double[2];   // new location suggested by smoothing
            double origin_x = 0, origin_y = 0; // for keeping torg safe
            Otri delotri; // keeping the original orientation for relocation process
            // keeps the first and second direction suggested points
            double dxFirstSuggestion, dyFirstSuggestion, dxSecondSuggestion, dySecondSuggestion;
            // second direction variables
            double xMidOfMiddleEdge, yMidOfMiddleEdge;
            ////////////////////////////// END OF HALE'S VARIABLES //////////////////////////////

            Statistic.CircumcenterCount++;

            // Compute the circumcenter of the triangle.
            xdo = tdest.x - torg.x;
            ydo = tdest.y - torg.y;
            xao = tapex.x - torg.x;
            yao = tapex.y - torg.y;
            xda = tapex.x - tdest.x;
            yda = tapex.y - tdest.y;
            // keeps the square of the distances
            dodist = xdo * xdo + ydo * ydo;
            aodist = xao * xao + yao * yao;
            dadist = (tdest.x - tapex.x) * (tdest.x - tapex.x) +
                (tdest.y - tapex.y) * (tdest.y - tapex.y);
            // checking if the user wanted exact arithmetic or not
            if (Behavior.NoExact)
            {
                denominator = 0.5 / (xdo * yao - xao * ydo);
            }
            else
            {
                // Use the counterclockwise() routine to ensure a positive (and
                //   reasonably accurate) result, avoiding any possibility of
                //   division by zero.
                denominator = 0.5 / predicates.CounterClockwise(tdest, tapex, torg);
                // Don't count the above as an orientation test.
                Statistic.CounterClockwiseCount--;
            }
            // calculate the circumcenter in terms of distance to origin point 
            dx = (yao * dodist - ydo * aodist) * denominator;
            dy = (xdo * aodist - xao * dodist) * denominator;
            // for debugging and for keeping circumcenter to use later
            // coordinate value of the circumcenter
            myCircumcenter = new Point(torg.x + dx, torg.y + dy);

            delotri = badotri; // save for later
            ///////////////// FINDING THE ORIENTATION OF TRIANGLE //////////////////
            // Find the (squared) length of the triangle's shortest edge.  This
            //   serves as a conservative estimate of the insertion radius of the
            //   circumcenter's parent.  The estimate is used to ensure that
            //   the algorithm terminates even if very small angles appear in
            //   the input PSLG. 						
            // find the orientation of the triangle, basically shortest and longest edges
            orientation = LongestShortestEdge(aodist, dadist, dodist);
            //printf("org: (%f,%f), dest: (%f,%f), apex: (%f,%f)\n",torg[0],torg[1],tdest[0],tdest[1],tapex[0],tapex[1]);
            /////////////////////////////////////////////////////////////////////////////////////////////
            // 123: shortest: aodist	// 213: shortest: dadist	// 312: shortest: dodist   //	
            //	middle: dadist 		//	middle: aodist 		//	middle: aodist     //
            //	longest: dodist		//	longest: dodist		//	longest: dadist    //
            // 132: shortest: aodist 	// 231: shortest: dadist 	// 321: shortest: dodist   //
            //	middle: dodist 		//	middle: dodist 		//	middle: dadist     //
            //	longest: dadist		//	longest: aodist		//	longest: aodist    //
            /////////////////////////////////////////////////////////////////////////////////////////////

            switch (orientation)
            {
                case 123: 	// assign necessary information
                    /// smallest angle corner: dest
                    /// largest angle corner: apex
                    xShortestEdge = xao; yShortestEdge = yao;

                    shortestEdgeDist = aodist;
                    middleEdgeDist = dadist;
                    longestEdgeDist = dodist;

                    smallestAngleCorner = tdest;
                    middleAngleCorner = torg;
                    largestAngleCorner = tapex;
                    break;

                case 132: 	// assign necessary information
                    /// smallest angle corner: dest
                    /// largest angle corner: org
                    xShortestEdge = xao; yShortestEdge = yao;

                    shortestEdgeDist = aodist;
                    middleEdgeDist = dodist;
                    longestEdgeDist = dadist;

                    smallestAngleCorner = tdest;
                    middleAngleCorner = tapex;
                    largestAngleCorner = torg;

                    break;
                case 213: 	// assign necessary information
                    /// smallest angle corner: org
                    /// largest angle corner: apex
                    xShortestEdge = xda; yShortestEdge = yda;

                    shortestEdgeDist = dadist;
                    middleEdgeDist = aodist;
                    longestEdgeDist = dodist;

                    smallestAngleCorner = torg;
                    middleAngleCorner = tdest;
                    largestAngleCorner = tapex;
                    break;
                case 231: 	// assign necessary information
                    /// smallest angle corner: org
                    /// largest angle corner: dest
                    xShortestEdge = xda; yShortestEdge = yda;

                    shortestEdgeDist = dadist;
                    middleEdgeDist = dodist;
                    longestEdgeDist = aodist;

                    smallestAngleCorner = torg;
                    middleAngleCorner = tapex;
                    largestAngleCorner = tdest;
                    break;
                case 312: 	// assign necessary information
                    /// smallest angle corner: apex
                    /// largest angle corner: org
                    xShortestEdge = xdo; yShortestEdge = ydo;

                    shortestEdgeDist = dodist;
                    middleEdgeDist = aodist;
                    longestEdgeDist = dadist;

                    smallestAngleCorner = tapex;
                    middleAngleCorner = tdest;
                    largestAngleCorner = torg;
                    break;
                case 321: 	// assign necessary information
                default: // TODO: is this safe?
                    /// smallest angle corner: apex
                    /// largest angle corner: dest
                    xShortestEdge = xdo; yShortestEdge = ydo;

                    shortestEdgeDist = dodist;
                    middleEdgeDist = dadist;
                    longestEdgeDist = aodist;

                    smallestAngleCorner = tapex;
                    middleAngleCorner = torg;
                    largestAngleCorner = tdest;
                    break;

            }// end of switch	
            // check for offcenter condition
            if (offcenter && (offconstant > 0.0))
            {
                // origin has the smallest angle
                if (orientation == 213 || orientation == 231)
                {
                    // Find the position of the off-center, as described by Alper Ungor.
                    dxoff = 0.5 * xShortestEdge - offconstant * yShortestEdge;
                    dyoff = 0.5 * yShortestEdge + offconstant * xShortestEdge;
                    // If the off-center is closer to destination than the
                    //   circumcenter, use the off-center instead.
                    /// doubleLY BAD CASE ///			
                    if (dxoff * dxoff + dyoff * dyoff <
                        (dx - xdo) * (dx - xdo) + (dy - ydo) * (dy - ydo))
                    {
                        dx = xdo + dxoff;
                        dy = ydo + dyoff;
                    }
                    /// ALMOST GOOD CASE ///
                    else
                    {
                        almostGood = 1;
                    }
                    // destination has the smallest angle	
                }
                else if (orientation == 123 || orientation == 132)
                {
                    // Find the position of the off-center, as described by Alper Ungor.
                    dxoff = 0.5 * xShortestEdge + offconstant * yShortestEdge;
                    dyoff = 0.5 * yShortestEdge - offconstant * xShortestEdge;
                    // If the off-center is closer to the origin than the
                    //   circumcenter, use the off-center instead.
                    /// doubleLY BAD CASE ///
                    if (dxoff * dxoff + dyoff * dyoff < dx * dx + dy * dy)
                    {
                        dx = dxoff;
                        dy = dyoff;
                    }
                    /// ALMOST GOOD CASE ///		
                    else
                    {
                        almostGood = 1;
                    }
                    // apex has the smallest angle	
                }
                else
                {//orientation == 312 || orientation == 321 
                    // Find the position of the off-center, as described by Alper Ungor.
                    dxoff = 0.5 * xShortestEdge - offconstant * yShortestEdge;
                    dyoff = 0.5 * yShortestEdge + offconstant * xShortestEdge;
                    // If the off-center is closer to the origin than the
                    //   circumcenter, use the off-center instead.
                    /// doubleLY BAD CASE ///
                    if (dxoff * dxoff + dyoff * dyoff < dx * dx + dy * dy)
                    {
                        dx = dxoff;
                        dy = dyoff;
                    }
                    /// ALMOST GOOD CASE ///		
                    else
                    {
                        almostGood = 1;
                    }
                }
            }
            // if the bad triangle is almost good, apply our approach
            if (almostGood == 1)
            {

                /// calculate cosine of largest angle	///	
                cosMaxAngle = (middleEdgeDist + shortestEdgeDist - longestEdgeDist) / (2 * Math.Sqrt(middleEdgeDist) * Math.Sqrt(shortestEdgeDist));
                if (cosMaxAngle < 0.0)
                {
                    // obtuse
                    isObtuse = true;
                }
                else if (Math.Abs(cosMaxAngle - 0.0) <= EPS)
                {
                    // right triangle (largest angle is 90 degrees)
                    isObtuse = true;
                }
                else
                {
                    // nonobtuse
                    isObtuse = false;
                }
                /// RELOCATION	(LOCAL SMOOTHING) ///
                /// check for possible relocation of one of triangle's points ///				
                relocated = DoSmoothing(delotri, torg, tdest, tapex, ref newloc);
                /// if relocation is possible, delete that vertex and insert a vertex at the new location ///		
                if (relocated > 0)
                {
                    Statistic.RelocationCount++;

                    dx = newloc[0] - torg.x;
                    dy = newloc[1] - torg.y;
                    origin_x = torg.x;	// keep for later use
                    origin_y = torg.y;
                    switch (relocated)
                    {
                        case 1:
                            //printf("Relocate: (%f,%f)\n", torg[0],torg[1]);			
                            mesh.DeleteVertex(ref delotri);
                            break;
                        case 2:
                            //printf("Relocate: (%f,%f)\n", tdest[0],tdest[1]);			
                            delotri.Lnext();
                            mesh.DeleteVertex(ref delotri);
                            break;
                        case 3:
                            //printf("Relocate: (%f,%f)\n", tapex[0],tapex[1]);						
                            delotri.Lprev();
                            mesh.DeleteVertex(ref delotri);
                            break;

                    }
                }
                else
                {
                    // calculate radius of the petal according to angle constraint
                    // first find the visible region, PETAL
                    // find the center of the circle and radius
                    petalRadius = Math.Sqrt(shortestEdgeDist) / (2 * Math.Sin(behavior.MinAngle * Math.PI / 180.0));
                    /// compute two possible centers of the petal ///
                    // finding the center
                    // first find the middle point of smallest edge
                    xMidOfShortestEdge = (middleAngleCorner.x + largestAngleCorner.x) / 2.0;
                    yMidOfShortestEdge = (middleAngleCorner.y + largestAngleCorner.y) / 2.0;
                    // two possible centers
                    xPetalCtr_1 = xMidOfShortestEdge + Math.Sqrt(petalRadius * petalRadius - (shortestEdgeDist / 4)) * (middleAngleCorner.y -
                        largestAngleCorner.y) / Math.Sqrt(shortestEdgeDist);
                    yPetalCtr_1 = yMidOfShortestEdge + Math.Sqrt(petalRadius * petalRadius - (shortestEdgeDist / 4)) * (largestAngleCorner.x -
                        middleAngleCorner.x) / Math.Sqrt(shortestEdgeDist);

                    xPetalCtr_2 = xMidOfShortestEdge - Math.Sqrt(petalRadius * petalRadius - (shortestEdgeDist / 4)) * (middleAngleCorner.y -
                        largestAngleCorner.y) / Math.Sqrt(shortestEdgeDist);
                    yPetalCtr_2 = yMidOfShortestEdge - Math.Sqrt(petalRadius * petalRadius - (shortestEdgeDist / 4)) * (largestAngleCorner.x -
                        middleAngleCorner.x) / Math.Sqrt(shortestEdgeDist);
                    // find the correct circle since there will be two possible circles
                    // calculate the distance to smallest angle corner
                    dxcenter1 = (xPetalCtr_1 - smallestAngleCorner.x) * (xPetalCtr_1 - smallestAngleCorner.x);
                    dycenter1 = (yPetalCtr_1 - smallestAngleCorner.y) * (yPetalCtr_1 - smallestAngleCorner.y);
                    dxcenter2 = (xPetalCtr_2 - smallestAngleCorner.x) * (xPetalCtr_2 - smallestAngleCorner.x);
                    dycenter2 = (yPetalCtr_2 - smallestAngleCorner.y) * (yPetalCtr_2 - smallestAngleCorner.y);

                    // whichever is closer to smallest angle corner, it must be the center
                    if (dxcenter1 + dycenter1 <= dxcenter2 + dycenter2)
                    {
                        xPetalCtr = xPetalCtr_1; yPetalCtr = yPetalCtr_1;
                    }
                    else
                    {
                        xPetalCtr = xPetalCtr_2; yPetalCtr = yPetalCtr_2;
                    }

                    /// find the third point of the neighbor triangle  ///
                    neighborNotFound = GetNeighborsVertex(badotri, middleAngleCorner.x, middleAngleCorner.y,
                                smallestAngleCorner.x, smallestAngleCorner.y, ref thirdPoint, ref neighborotri);
                    /// find the circumcenter of the neighbor triangle ///
                    dxFirstSuggestion = dx;	// if we cannot find any appropriate suggestion, we use circumcenter
                    dyFirstSuggestion = dy;
                    // if there is a neighbor triangle
                    if (!neighborNotFound)
                    {
                        neighborvertex_1 = neighborotri.Org();
                        neighborvertex_2 = neighborotri.Dest();
                        neighborvertex_3 = neighborotri.Apex();
                        // now calculate neighbor's circumcenter which is the voronoi site
                        neighborCircumcenter = predicates.FindCircumcenter(neighborvertex_1, neighborvertex_2, neighborvertex_3,
                            ref xi_tmp, ref eta_tmp);

                        /// compute petal and Voronoi edge intersection ///
                        // in order to avoid degenerate cases, we need to do a vector based calculation for line		
                        vector_x = (middleAngleCorner.y - smallestAngleCorner.y);//(-y, x)
                        vector_y = smallestAngleCorner.x - middleAngleCorner.x;
                        vector_x = myCircumcenter.x + vector_x;
                        vector_y = myCircumcenter.y + vector_y;


                        // by intersecting bisectors you will end up with the one you want to walk on
                        // then this line and circle should be intersected
                        CircleLineIntersection(myCircumcenter.x, myCircumcenter.y, vector_x, vector_y,
                                xPetalCtr, yPetalCtr, petalRadius, ref p);
                        /// choose the correct intersection point ///
                        // calculate middle point of the longest edge(bisector)
                        xMidOfLongestEdge = (middleAngleCorner.x + smallestAngleCorner.x) / 2.0;
                        yMidOfLongestEdge = (middleAngleCorner.y + smallestAngleCorner.y) / 2.0;
                        // we need to find correct intersection point, since line intersects circle twice
                        isCorrect = ChooseCorrectPoint(xMidOfLongestEdge, yMidOfLongestEdge, p[3], p[4],
                                    myCircumcenter.x, myCircumcenter.y, isObtuse);
                        // make sure which point is the correct one to be considered
                        if (isCorrect)
                        {
                            inter_x = p[3];
                            inter_y = p[4];
                        }
                        else
                        {
                            inter_x = p[1];
                            inter_y = p[2];
                        }
                        /// check if there is a Voronoi vertex between before intersection ///
                        // check if the voronoi vertex is between the intersection and circumcenter
                        PointBetweenPoints(inter_x, inter_y, myCircumcenter.x, myCircumcenter.y,
                                neighborCircumcenter.x, neighborCircumcenter.y, ref voronoiOrInter);

                        /// determine the point to be suggested ///
                        if (p[0] > 0.0)
                        { // there is at least one intersection point
                            // if it is between circumcenter and intersection	
                            // if it returns 1.0 this means we have a voronoi vertex within feasible region
                            if (Math.Abs(voronoiOrInter[0] - 1.0) <= EPS)
                            {
                                if (IsBadTriangleAngle(middleAngleCorner.x, middleAngleCorner.y, largestAngleCorner.x, largestAngleCorner.y, neighborCircumcenter.x, neighborCircumcenter.y))
                                {
                                    // go back to circumcenter
                                    dxFirstSuggestion = dx;
                                    dyFirstSuggestion = dy;

                                }
                                else
                                { // we are not creating a bad triangle
                                    // neighbor's circumcenter is suggested
                                    dxFirstSuggestion = voronoiOrInter[2] - torg.x;
                                    dyFirstSuggestion = voronoiOrInter[3] - torg.y;
                                }

                            }
                            else
                            { // there is no voronoi vertex between intersection point and circumcenter
                                if (IsBadTriangleAngle(largestAngleCorner.x, largestAngleCorner.y, middleAngleCorner.x, middleAngleCorner.y, inter_x, inter_y))
                                {
                                    // if it is inside feasible region, then insert v2				
                                    // apply perturbation
                                    // find the distance between circumcenter and intersection point
                                    d = Math.Sqrt((inter_x - myCircumcenter.x) * (inter_x - myCircumcenter.x) +
                                        (inter_y - myCircumcenter.y) * (inter_y - myCircumcenter.y));
                                    // then find the vector going from intersection point to circumcenter
                                    ax = myCircumcenter.x - inter_x;
                                    ay = myCircumcenter.y - inter_y;

                                    ax = ax / d;
                                    ay = ay / d;
                                    // now calculate the new intersection point which is perturbated towards the circumcenter
                                    inter_x = inter_x + ax * pertConst * Math.Sqrt(shortestEdgeDist);
                                    inter_y = inter_y + ay * pertConst * Math.Sqrt(shortestEdgeDist);
                                    if (IsBadTriangleAngle(middleAngleCorner.x, middleAngleCorner.y, largestAngleCorner.x, largestAngleCorner.y, inter_x, inter_y))
                                    {
                                        // go back to circumcenter
                                        dxFirstSuggestion = dx;
                                        dyFirstSuggestion = dy;

                                    }
                                    else
                                    {
                                        // intersection point is suggested
                                        dxFirstSuggestion = inter_x - torg.x;
                                        dyFirstSuggestion = inter_y - torg.y;

                                    }
                                }
                                else
                                {
                                    // intersection point is suggested
                                    dxFirstSuggestion = inter_x - torg.x;
                                    dyFirstSuggestion = inter_y - torg.y;
                                }
                            }
                            /// if it is an acute triangle, check if it is a good enough location ///
                            // for acute triangle case, we need to check if it is ok to use either of them
                            if ((smallestAngleCorner.x - myCircumcenter.x) * (smallestAngleCorner.x - myCircumcenter.x) +
                                (smallestAngleCorner.y - myCircumcenter.y) * (smallestAngleCorner.y - myCircumcenter.y) >
                                lengthConst * ((smallestAngleCorner.x - (dxFirstSuggestion + torg.x)) *
                                        (smallestAngleCorner.x - (dxFirstSuggestion + torg.x)) +
                                        (smallestAngleCorner.y - (dyFirstSuggestion + torg.y)) *
                                        (smallestAngleCorner.y - (dyFirstSuggestion + torg.y))))
                            {
                                // use circumcenter
                                dxFirstSuggestion = dx;
                                dyFirstSuggestion = dy;
                            }// else we stick to what we have found	
                        }// intersection point

                    }// if it is on the boundary, meaning no neighbor triangle in this direction, try other direction	

                    /// DO THE SAME THING FOR THE OTHER DIRECTION ///
                    /// find the third point of the neighbor triangle  ///
                    neighborNotFound = GetNeighborsVertex(badotri, largestAngleCorner.x, largestAngleCorner.y,
                                smallestAngleCorner.x, smallestAngleCorner.y, ref thirdPoint, ref neighborotri);
                    /// find the circumcenter of the neighbor triangle ///
                    dxSecondSuggestion = dx;	// if we cannot find any appropriate suggestion, we use circumcenter
                    dySecondSuggestion = dy;
                    // if there is a neighbor triangle
                    if (!neighborNotFound)
                    {
                        neighborvertex_1 = neighborotri.Org();
                        neighborvertex_2 = neighborotri.Dest();
                        neighborvertex_3 = neighborotri.Apex();
                        // now calculate neighbor's circumcenter which is the voronoi site
                        neighborCircumcenter = predicates.FindCircumcenter(neighborvertex_1, neighborvertex_2, neighborvertex_3,
                            ref xi_tmp, ref eta_tmp);

                        /// compute petal and Voronoi edge intersection ///
                        // in order to avoid degenerate cases, we need to do a vector based calculation for line		
                        vector_x = (largestAngleCorner.y - smallestAngleCorner.y);//(-y, x)
                        vector_y = smallestAngleCorner.x - largestAngleCorner.x;
                        vector_x = myCircumcenter.x + vector_x;
                        vector_y = myCircumcenter.y + vector_y;


                        // by intersecting bisectors you will end up with the one you want to walk on
                        // then this line and circle should be intersected
                        CircleLineIntersection(myCircumcenter.x, myCircumcenter.y, vector_x, vector_y,
                                xPetalCtr, yPetalCtr, petalRadius, ref p);

                        /// choose the correct intersection point ///
                        // calcuwedgeslate middle point of the longest edge(bisector)
                        xMidOfMiddleEdge = (largestAngleCorner.x + smallestAngleCorner.x) / 2.0;
                        yMidOfMiddleEdge = (largestAngleCorner.y + smallestAngleCorner.y) / 2.0;
                        // we need to find correct intersection point, since line intersects circle twice
                        // this direction is always ACUTE
                        isCorrect = ChooseCorrectPoint(xMidOfMiddleEdge, yMidOfMiddleEdge, p[3], p[4],
                                    myCircumcenter.x, myCircumcenter.y, false/*(isObtuse+1)%2*/);
                        // make sure which point is the correct one to be considered
                        if (isCorrect)
                        {
                            inter_x = p[3];
                            inter_y = p[4];
                        }
                        else
                        {
                            inter_x = p[1];
                            inter_y = p[2];
                        }

                        /// check if there is a Voronoi vertex between before intersection ///
                        // check if the voronoi vertex is between the intersection and circumcenter
                        PointBetweenPoints(inter_x, inter_y, myCircumcenter.x, myCircumcenter.y,
                                neighborCircumcenter.x, neighborCircumcenter.y, ref voronoiOrInter);

                        /// determine the point to be suggested ///
                        if (p[0] > 0.0)
                        { // there is at least one intersection point
                            // if it is between circumcenter and intersection	
                            // if it returns 1.0 this means we have a voronoi vertex within feasible region
                            if (Math.Abs(voronoiOrInter[0] - 1.0) <= EPS)
                            {
                                if (IsBadTriangleAngle(middleAngleCorner.x, middleAngleCorner.y, largestAngleCorner.x, largestAngleCorner.y, neighborCircumcenter.x, neighborCircumcenter.y))
                                {
                                    // go back to circumcenter
                                    dxSecondSuggestion = dx;
                                    dySecondSuggestion = dy;

                                }
                                else
                                { // we are not creating a bad triangle
                                    // neighbor's circumcenter is suggested
                                    dxSecondSuggestion = voronoiOrInter[2] - torg.x;
                                    dySecondSuggestion = voronoiOrInter[3] - torg.y;

                                }

                            }
                            else
                            { // there is no voronoi vertex between intersection point and circumcenter
                                if (IsBadTriangleAngle(middleAngleCorner.x, middleAngleCorner.y, largestAngleCorner.x, largestAngleCorner.y, inter_x, inter_y))
                                {
                                    // if it is inside feasible region, then insert v2				
                                    // apply perturbation
                                    // find the distance between circumcenter and intersection point
                                    d = Math.Sqrt((inter_x - myCircumcenter.x) * (inter_x - myCircumcenter.x) +
                                        (inter_y - myCircumcenter.y) * (inter_y - myCircumcenter.y));
                                    // then find the vector going from intersection point to circumcenter
                                    ax = myCircumcenter.x - inter_x;
                                    ay = myCircumcenter.y - inter_y;

                                    ax = ax / d;
                                    ay = ay / d;
                                    // now calculate the new intersection point which is perturbated towards the circumcenter
                                    inter_x = inter_x + ax * pertConst * Math.Sqrt(shortestEdgeDist);
                                    inter_y = inter_y + ay * pertConst * Math.Sqrt(shortestEdgeDist);
                                    if (IsBadTriangleAngle(middleAngleCorner.x, middleAngleCorner.y, largestAngleCorner.x, largestAngleCorner.y, inter_x, inter_y))
                                    {
                                        // go back to circumcenter
                                        dxSecondSuggestion = dx;
                                        dySecondSuggestion = dy;

                                    }
                                    else
                                    {
                                        // intersection point is suggested
                                        dxSecondSuggestion = inter_x - torg.x;
                                        dySecondSuggestion = inter_y - torg.y;
                                    }
                                }
                                else
                                {

                                    // intersection point is suggested
                                    dxSecondSuggestion = inter_x - torg.x;
                                    dySecondSuggestion = inter_y - torg.y;
                                }
                            }
                            /// if it is an acute triangle, check if it is a good enough location ///
                            // for acute triangle case, we need to check if it is ok to use either of them
                            if ((smallestAngleCorner.x - myCircumcenter.x) * (smallestAngleCorner.x - myCircumcenter.x) +
                                (smallestAngleCorner.y - myCircumcenter.y) * (smallestAngleCorner.y - myCircumcenter.y) >
                                lengthConst * ((smallestAngleCorner.x - (dxSecondSuggestion + torg.x)) *
                                        (smallestAngleCorner.x - (dxSecondSuggestion + torg.x)) +
                                        (smallestAngleCorner.y - (dySecondSuggestion + torg.y)) *
                                        (smallestAngleCorner.y - (dySecondSuggestion + torg.y))))
                            {
                                // use circumcenter
                                dxSecondSuggestion = dx;
                                dySecondSuggestion = dy;
                            }// else we stick on what we have found	
                        }
                    }// if it is on the boundary, meaning no neighbor triangle in this direction, the other direction might be ok		
                    if (isObtuse)
                    {
                        //obtuse: do nothing					
                        dx = dxFirstSuggestion;
                        dy = dyFirstSuggestion;
                    }
                    else
                    { // acute : consider other direction				
                        if (justAcute * ((smallestAngleCorner.x - (dxSecondSuggestion + torg.x)) *
                                (smallestAngleCorner.x - (dxSecondSuggestion + torg.x)) +
                                (smallestAngleCorner.y - (dySecondSuggestion + torg.y)) *
                                (smallestAngleCorner.y - (dySecondSuggestion + torg.y))) >
                                (smallestAngleCorner.x - (dxFirstSuggestion + torg.x)) *
                                (smallestAngleCorner.x - (dxFirstSuggestion + torg.x)) +
                                (smallestAngleCorner.y - (dyFirstSuggestion + torg.y)) *
                                (smallestAngleCorner.y - (dyFirstSuggestion + torg.y)))
                        {
                            dx = dxSecondSuggestion;
                            dy = dySecondSuggestion;
                        }
                        else
                        {
                            dx = dxFirstSuggestion;
                            dy = dyFirstSuggestion;
                        }

                    }// end if obtuse
                }// end of relocation				 
            }// end of almostGood	

            Point circumcenter = new Point();

            if (relocated <= 0)
            {
                circumcenter.x = torg.x + dx;
                circumcenter.y = torg.y + dy;
            }
            else
            {
                circumcenter.x = origin_x + dx;
                circumcenter.y = origin_y + dy;
            }

            xi = (yao * dx - xao * dy) * (2.0 * denominator);
            eta = (xdo * dy - ydo * dx) * (2.0 * denominator);

            return circumcenter;
        }

        /// <summary>
        /// Find a new location for a Steiner point.
        /// </summary>
        /// <param name="torg"></param>
        /// <param name="tdest"></param>
        /// <param name="tapex"></param>
        /// <param name="circumcenter"></param>
        /// <param name="xi"></param>
        /// <param name="eta"></param>
        /// <param name="offcenter"></param>
        /// <param name="badotri"></param>
        private Point FindNewLocation(Vertex torg, Vertex tdest, Vertex tapex,
            ref double xi, ref double eta, bool offcenter, Otri badotri)
        {
            double offconstant = behavior.offconstant;

            // for calculating the distances of the edges
            double xdo, ydo, xao, yao, xda, yda;
            double dodist, aodist, dadist;
            // for exact calculation
            double denominator;
            double dx, dy, dxoff, dyoff;

            ////////////////////////////// HALE'S VARIABLES //////////////////////////////
            // keeps the difference of coordinates edge 
            double xShortestEdge = 0, yShortestEdge = 0;

            // keeps the square of edge lengths
            double shortestEdgeDist = 0, middleEdgeDist = 0, longestEdgeDist = 0;

            // keeps the vertices according to the angle incident to that vertex in a triangle
            Point smallestAngleCorner, middleAngleCorner, largestAngleCorner;

            // keeps the type of orientation if the triangle
            int orientation = 0;
            // keeps the coordinates of circumcenter of itself and neighbor triangle circumcenter	
            Point myCircumcenter, neighborCircumcenter;

            // keeps if bad triangle is almost good or not
            int almostGood = 0;
            // keeps the cosine of the largest angle
            double cosMaxAngle;
            bool isObtuse; // 1: obtuse 0: nonobtuse
            // keeps the radius of petal
            double petalRadius;
            // for calculating petal center
            double xPetalCtr_1, yPetalCtr_1, xPetalCtr_2, yPetalCtr_2, xPetalCtr, yPetalCtr, xMidOfShortestEdge, yMidOfShortestEdge;
            double dxcenter1, dycenter1, dxcenter2, dycenter2;
            // for finding neighbor
            Otri neighborotri = default(Otri);
            double[] thirdPoint = new double[2];
            //int neighborNotFound = -1;
            // for keeping the vertices of the neighbor triangle
            Vertex neighborvertex_1;
            Vertex neighborvertex_2;
            Vertex neighborvertex_3;
            // dummy variables 
            double xi_tmp = 0, eta_tmp = 0;
            //vertex thirdVertex;
            // for petal intersection
            double vector_x, vector_y, xMidOfLongestEdge, yMidOfLongestEdge, inter_x, inter_y;
            double[] p = new double[5], voronoiOrInter = new double[4];
            bool isCorrect;

            // for vector calculations in perturbation
            double ax, ay, d;
            double pertConst = 0.06; // perturbation constant

            double lengthConst = 1; // used at comparing circumcenter's distance to proposed point's distance
            double justAcute = 1; // used for making the program working for one direction only
            // for smoothing
            int relocated = 0;// used to differentiate between calling the deletevertex and just proposing a steiner point
            double[] newloc = new double[2];   // new location suggested by smoothing
            double origin_x = 0, origin_y = 0; // for keeping torg safe
            Otri delotri; // keeping the original orientation for relocation process
            // keeps the first and second direction suggested points
            double dxFirstSuggestion, dyFirstSuggestion, dxSecondSuggestion, dySecondSuggestion;
            // second direction variables
            double xMidOfMiddleEdge, yMidOfMiddleEdge;

            double minangle;	// in order to make sure that the circumcircle of the bad triangle is greater than petal
            // for calculating the slab
            double linepnt1_x, linepnt1_y, linepnt2_x, linepnt2_y;	// two points of the line
            double line_inter_x = 0, line_inter_y = 0;
            double line_vector_x, line_vector_y;
            double[] line_p = new double[3]; // used for getting the return values of functions related to line intersection
            double[] line_result = new double[4];
            // intersection of slab and the petal
            double petal_slab_inter_x_first, petal_slab_inter_y_first, petal_slab_inter_x_second, petal_slab_inter_y_second, x_1, y_1, x_2, y_2;
            double petal_bisector_x, petal_bisector_y, dist;
            double alpha;
            bool neighborNotFound_first;
            bool neighborNotFound_second;
            ////////////////////////////// END OF HALE'S VARIABLES //////////////////////////////

            Statistic.CircumcenterCount++;

            // Compute the circumcenter of the triangle.
            xdo = tdest.x - torg.x;
            ydo = tdest.y - torg.y;
            xao = tapex.x - torg.x;
            yao = tapex.y - torg.y;
            xda = tapex.x - tdest.x;
            yda = tapex.y - tdest.y;
            // keeps the square of the distances
            dodist = xdo * xdo + ydo * ydo;
            aodist = xao * xao + yao * yao;
            dadist = (tdest.x - tapex.x) * (tdest.x - tapex.x) +
                (tdest.y - tapex.y) * (tdest.y - tapex.y);
            // checking if the user wanted exact arithmetic or not
            if (Behavior.NoExact)
            {
                denominator = 0.5 / (xdo * yao - xao * ydo);
            }
            else
            {
                // Use the counterclockwise() routine to ensure a positive (and
                //   reasonably accurate) result, avoiding any possibility of
                //   division by zero.
                denominator = 0.5 / predicates.CounterClockwise(tdest, tapex, torg);
                // Don't count the above as an orientation test.
                Statistic.CounterClockwiseCount--;
            }
            // calculate the circumcenter in terms of distance to origin point 
            dx = (yao * dodist - ydo * aodist) * denominator;
            dy = (xdo * aodist - xao * dodist) * denominator;
            // for debugging and for keeping circumcenter to use later
            // coordinate value of the circumcenter
            myCircumcenter = new Point(torg.x + dx, torg.y + dy);

            delotri = badotri; // save for later
            ///////////////// FINDING THE ORIENTATION OF TRIANGLE //////////////////
            // Find the (squared) length of the triangle's shortest edge.  This
            //   serves as a conservative estimate of the insertion radius of the
            //   circumcenter's parent.  The estimate is used to ensure that
            //   the algorithm terminates even if very small angles appear in
            //   the input PSLG. 						
            // find the orientation of the triangle, basically shortest and longest edges
            orientation = LongestShortestEdge(aodist, dadist, dodist);
            //printf("org: (%f,%f), dest: (%f,%f), apex: (%f,%f)\n",torg[0],torg[1],tdest[0],tdest[1],tapex[0],tapex[1]);
            /////////////////////////////////////////////////////////////////////////////////////////////
            // 123: shortest: aodist	// 213: shortest: dadist	// 312: shortest: dodist   //	
            //	middle: dadist 		//	middle: aodist 		//	middle: aodist     //
            //	longest: dodist		//	longest: dodist		//	longest: dadist    //
            // 132: shortest: aodist 	// 231: shortest: dadist 	// 321: shortest: dodist   //
            //	middle: dodist 		//	middle: dodist 		//	middle: dadist     //
            //	longest: dadist		//	longest: aodist		//	longest: aodist    //
            /////////////////////////////////////////////////////////////////////////////////////////////

            switch (orientation)
            {
                case 123: 	// assign necessary information
                    /// smallest angle corner: dest
                    /// largest angle corner: apex
                    xShortestEdge = xao; yShortestEdge = yao;

                    shortestEdgeDist = aodist;
                    middleEdgeDist = dadist;
                    longestEdgeDist = dodist;

                    smallestAngleCorner = tdest;
                    middleAngleCorner = torg;
                    largestAngleCorner = tapex;
                    break;

                case 132: 	// assign necessary information
                    /// smallest angle corner: dest
                    /// largest angle corner: org
                    xShortestEdge = xao; yShortestEdge = yao;

                    shortestEdgeDist = aodist;
                    middleEdgeDist = dodist;
                    longestEdgeDist = dadist;

                    smallestAngleCorner = tdest;
                    middleAngleCorner = tapex;
                    largestAngleCorner = torg;

                    break;
                case 213: 	// assign necessary information
                    /// smallest angle corner: org
                    /// largest angle corner: apex
                    xShortestEdge = xda; yShortestEdge = yda;

                    shortestEdgeDist = dadist;
                    middleEdgeDist = aodist;
                    longestEdgeDist = dodist;

                    smallestAngleCorner = torg;
                    middleAngleCorner = tdest;
                    largestAngleCorner = tapex;
                    break;
                case 231: 	// assign necessary information
                    /// smallest angle corner: org
                    /// largest angle corner: dest
                    xShortestEdge = xda; yShortestEdge = yda;

                    shortestEdgeDist = dadist;
                    middleEdgeDist = dodist;
                    longestEdgeDist = aodist;

                    smallestAngleCorner = torg;
                    middleAngleCorner = tapex;
                    largestAngleCorner = tdest;
                    break;
                case 312: 	// assign necessary information
                    /// smallest angle corner: apex
                    /// largest angle corner: org
                    xShortestEdge = xdo; yShortestEdge = ydo;

                    shortestEdgeDist = dodist;
                    middleEdgeDist = aodist;
                    longestEdgeDist = dadist;

                    smallestAngleCorner = tapex;
                    middleAngleCorner = tdest;
                    largestAngleCorner = torg;
                    break;
                case 321: 	// assign necessary information
                default: // TODO: is this safe?
                    /// smallest angle corner: apex
                    /// largest angle corner: dest
                    xShortestEdge = xdo; yShortestEdge = ydo;

                    shortestEdgeDist = dodist;
                    middleEdgeDist = dadist;
                    longestEdgeDist = aodist;

                    smallestAngleCorner = tapex;
                    middleAngleCorner = torg;
                    largestAngleCorner = tdest;
                    break;

            }// end of switch	
            // check for offcenter condition
            if (offcenter && (offconstant > 0.0))
            {
                // origin has the smallest angle
                if (orientation == 213 || orientation == 231)
                {
                    // Find the position of the off-center, as described by Alper Ungor.
                    dxoff = 0.5 * xShortestEdge - offconstant * yShortestEdge;
                    dyoff = 0.5 * yShortestEdge + offconstant * xShortestEdge;
                    // If the off-center is closer to destination than the
                    //   circumcenter, use the off-center instead.
                    /// doubleLY BAD CASE ///			
                    if (dxoff * dxoff + dyoff * dyoff <
                        (dx - xdo) * (dx - xdo) + (dy - ydo) * (dy - ydo))
                    {
                        dx = xdo + dxoff;
                        dy = ydo + dyoff;
                    }
                    /// ALMOST GOOD CASE ///
                    else
                    {
                        almostGood = 1;
                    }
                    // destination has the smallest angle	
                }
                else if (orientation == 123 || orientation == 132)
                {
                    // Find the position of the off-center, as described by Alper Ungor.
                    dxoff = 0.5 * xShortestEdge + offconstant * yShortestEdge;
                    dyoff = 0.5 * yShortestEdge - offconstant * xShortestEdge;
                    // If the off-center is closer to the origin than the
                    //   circumcenter, use the off-center instead.
                    /// doubleLY BAD CASE ///
                    if (dxoff * dxoff + dyoff * dyoff < dx * dx + dy * dy)
                    {
                        dx = dxoff;
                        dy = dyoff;
                    }
                    /// ALMOST GOOD CASE ///		
                    else
                    {
                        almostGood = 1;
                    }
                    // apex has the smallest angle	
                }
                else
                {//orientation == 312 || orientation == 321 
                    // Find the position of the off-center, as described by Alper Ungor.
                    dxoff = 0.5 * xShortestEdge - offconstant * yShortestEdge;
                    dyoff = 0.5 * yShortestEdge + offconstant * xShortestEdge;
                    // If the off-center is closer to the origin than the
                    //   circumcenter, use the off-center instead.
                    /// doubleLY BAD CASE ///
                    if (dxoff * dxoff + dyoff * dyoff < dx * dx + dy * dy)
                    {
                        dx = dxoff;
                        dy = dyoff;
                    }
                    /// ALMOST GOOD CASE ///		
                    else
                    {
                        almostGood = 1;
                    }
                }
            }
            // if the bad triangle is almost good, apply our approach
            if (almostGood == 1)
            {

                /// calculate cosine of largest angle	///	
                cosMaxAngle = (middleEdgeDist + shortestEdgeDist - longestEdgeDist) / (2 * Math.Sqrt(middleEdgeDist) * Math.Sqrt(shortestEdgeDist));
                if (cosMaxAngle < 0.0)
                {
                    // obtuse
                    isObtuse = true;
                }
                else if (Math.Abs(cosMaxAngle - 0.0) <= EPS)
                {
                    // right triangle (largest angle is 90 degrees)
                    isObtuse = true;
                }
                else
                {
                    // nonobtuse
                    isObtuse = false;
                }
                /// RELOCATION	(LOCAL SMOOTHING) ///
                /// check for possible relocation of one of triangle's points ///				
                relocated = DoSmoothing(delotri, torg, tdest, tapex, ref newloc);
                /// if relocation is possible, delete that vertex and insert a vertex at the new location ///		
                if (relocated > 0)
                {
                    Statistic.RelocationCount++;

                    dx = newloc[0] - torg.x;
                    dy = newloc[1] - torg.y;
                    origin_x = torg.x;	// keep for later use
                    origin_y = torg.y;
                    switch (relocated)
                    {
                        case 1:
                            //printf("Relocate: (%f,%f)\n", torg[0],torg[1]);			
                            mesh.DeleteVertex(ref delotri);
                            break;
                        case 2:
                            //printf("Relocate: (%f,%f)\n", tdest[0],tdest[1]);			
                            delotri.Lnext();
                            mesh.DeleteVertex(ref delotri);
                            break;
                        case 3:
                            //printf("Relocate: (%f,%f)\n", tapex[0],tapex[1]);						
                            delotri.Lprev();
                            mesh.DeleteVertex(ref delotri);
                            break;
                    }
                }
                else
                {
                    // calculate radius of the petal according to angle constraint
                    // first find the visible region, PETAL
                    // find the center of the circle and radius
                    // choose minimum angle as the maximum of quality angle and the minimum angle of the bad triangle
                    minangle = Math.Acos((middleEdgeDist + longestEdgeDist - shortestEdgeDist) / (2 * Math.Sqrt(middleEdgeDist) * Math.Sqrt(longestEdgeDist))) * 180.0 / Math.PI;
                    if (behavior.MinAngle > minangle)
                    {
                        minangle = behavior.MinAngle;
                    }
                    else
                    {
                        minangle = minangle + 0.5;
                    }
                    petalRadius = Math.Sqrt(shortestEdgeDist) / (2 * Math.Sin(minangle * Math.PI / 180.0));
                    /// compute two possible centers of the petal ///
                    // finding the center
                    // first find the middle point of smallest edge
                    xMidOfShortestEdge = (middleAngleCorner.x + largestAngleCorner.x) / 2.0;
                    yMidOfShortestEdge = (middleAngleCorner.y + largestAngleCorner.y) / 2.0;
                    // two possible centers
                    xPetalCtr_1 = xMidOfShortestEdge + Math.Sqrt(petalRadius * petalRadius - (shortestEdgeDist / 4)) * (middleAngleCorner.y -
                        largestAngleCorner.y) / Math.Sqrt(shortestEdgeDist);
                    yPetalCtr_1 = yMidOfShortestEdge + Math.Sqrt(petalRadius * petalRadius - (shortestEdgeDist / 4)) * (largestAngleCorner.x -
                        middleAngleCorner.x) / Math.Sqrt(shortestEdgeDist);

                    xPetalCtr_2 = xMidOfShortestEdge - Math.Sqrt(petalRadius * petalRadius - (shortestEdgeDist / 4)) * (middleAngleCorner.y -
                        largestAngleCorner.y) / Math.Sqrt(shortestEdgeDist);
                    yPetalCtr_2 = yMidOfShortestEdge - Math.Sqrt(petalRadius * petalRadius - (shortestEdgeDist / 4)) * (largestAngleCorner.x -
                        middleAngleCorner.x) / Math.Sqrt(shortestEdgeDist);
                    // find the correct circle since there will be two possible circles
                    // calculate the distance to smallest angle corner
                    dxcenter1 = (xPetalCtr_1 - smallestAngleCorner.x) * (xPetalCtr_1 - smallestAngleCorner.x);
                    dycenter1 = (yPetalCtr_1 - smallestAngleCorner.y) * (yPetalCtr_1 - smallestAngleCorner.y);
                    dxcenter2 = (xPetalCtr_2 - smallestAngleCorner.x) * (xPetalCtr_2 - smallestAngleCorner.x);
                    dycenter2 = (yPetalCtr_2 - smallestAngleCorner.y) * (yPetalCtr_2 - smallestAngleCorner.y);

                    // whichever is closer to smallest angle corner, it must be the center
                    if (dxcenter1 + dycenter1 <= dxcenter2 + dycenter2)
                    {
                        xPetalCtr = xPetalCtr_1; yPetalCtr = yPetalCtr_1;
                    }
                    else
                    {
                        xPetalCtr = xPetalCtr_2; yPetalCtr = yPetalCtr_2;
                    }
                    /// find the third point of the neighbor triangle  ///
                    neighborNotFound_first = GetNeighborsVertex(badotri, middleAngleCorner.x, middleAngleCorner.y,
                                smallestAngleCorner.x, smallestAngleCorner.y, ref thirdPoint, ref neighborotri);
                    /// find the circumcenter of the neighbor triangle ///
                    dxFirstSuggestion = dx;	// if we cannot find any appropriate suggestion, we use circumcenter
                    dyFirstSuggestion = dy;
                    /// before checking the neighbor, find the petal and slab intersections ///
                    // calculate the intersection point of the petal and the slab lines
                    // first find the vector			
                    // distance between xmid and petal center			
                    dist = Math.Sqrt((xPetalCtr - xMidOfShortestEdge) * (xPetalCtr - xMidOfShortestEdge) + (yPetalCtr - yMidOfShortestEdge) * (yPetalCtr - yMidOfShortestEdge));
                    // find the unit vector goes from mid point to petal center			
                    line_vector_x = (xPetalCtr - xMidOfShortestEdge) / dist;
                    line_vector_y = (yPetalCtr - yMidOfShortestEdge) / dist;
                    // find the third point other than p and q
                    petal_bisector_x = xPetalCtr + line_vector_x * petalRadius;
                    petal_bisector_y = yPetalCtr + line_vector_y * petalRadius;
                    alpha = (2.0 * behavior.MaxAngle + minangle - 180.0) * Math.PI / 180.0;
                    // rotate the vector cw around the petal center			
                    x_1 = petal_bisector_x * Math.Cos(alpha) + petal_bisector_y * Math.Sin(alpha) + xPetalCtr - xPetalCtr * Math.Cos(alpha) - yPetalCtr * Math.Sin(alpha);
                    y_1 = -petal_bisector_x * Math.Sin(alpha) + petal_bisector_y * Math.Cos(alpha) + yPetalCtr + xPetalCtr * Math.Sin(alpha) - yPetalCtr * Math.Cos(alpha);
                    // rotate the vector ccw around the petal center			
                    x_2 = petal_bisector_x * Math.Cos(alpha) - petal_bisector_y * Math.Sin(alpha) + xPetalCtr - xPetalCtr * Math.Cos(alpha) + yPetalCtr * Math.Sin(alpha);
                    y_2 = petal_bisector_x * Math.Sin(alpha) + petal_bisector_y * Math.Cos(alpha) + yPetalCtr - xPetalCtr * Math.Sin(alpha) - yPetalCtr * Math.Cos(alpha);
                    // we need to find correct intersection point, since there are two possibilities
                    // weather it is obtuse/acute the one closer to the minimum angle corner is the first direction
                    isCorrect = ChooseCorrectPoint(x_2, y_2, middleAngleCorner.x, middleAngleCorner.y, x_1, y_1, true);
                    // make sure which point is the correct one to be considered				
                    if (isCorrect)
                    {
                        petal_slab_inter_x_first = x_1;
                        petal_slab_inter_y_first = y_1;
                        petal_slab_inter_x_second = x_2;
                        petal_slab_inter_y_second = y_2;
                    }
                    else
                    {
                        petal_slab_inter_x_first = x_2;
                        petal_slab_inter_y_first = y_2;
                        petal_slab_inter_x_second = x_1;
                        petal_slab_inter_y_second = y_1;
                    }
                    /// choose the correct intersection point ///
                    // calculate middle point of the longest edge(bisector)
                    xMidOfLongestEdge = (middleAngleCorner.x + smallestAngleCorner.x) / 2.0;
                    yMidOfLongestEdge = (middleAngleCorner.y + smallestAngleCorner.y) / 2.0;
                    // if there is a neighbor triangle
                    if (!neighborNotFound_first)
                    {
                        neighborvertex_1 = neighborotri.Org();
                        neighborvertex_2 = neighborotri.Dest();
                        neighborvertex_3 = neighborotri.Apex();
                        // now calculate neighbor's circumcenter which is the voronoi site
                        neighborCircumcenter = predicates.FindCircumcenter(neighborvertex_1, neighborvertex_2, neighborvertex_3,
                            ref xi_tmp, ref eta_tmp);

                        /// compute petal and Voronoi edge intersection ///						
                        // in order to avoid degenerate cases, we need to do a vector based calculation for line		
                        vector_x = (middleAngleCorner.y - smallestAngleCorner.y);//(-y, x)
                        vector_y = smallestAngleCorner.x - middleAngleCorner.x;
                        vector_x = myCircumcenter.x + vector_x;
                        vector_y = myCircumcenter.y + vector_y;
                        // by intersecting bisectors you will end up with the one you want to walk on
                        // then this line and circle should be intersected
                        CircleLineIntersection(myCircumcenter.x, myCircumcenter.y, vector_x, vector_y,
                                xPetalCtr, yPetalCtr, petalRadius, ref p);
                        // we need to find correct intersection point, since line intersects circle twice
                        isCorrect = ChooseCorrectPoint(xMidOfLongestEdge, yMidOfLongestEdge, p[3], p[4],
                                    myCircumcenter.x, myCircumcenter.y, isObtuse);
                        // make sure which point is the correct one to be considered
                        if (isCorrect)
                        {
                            inter_x = p[3];
                            inter_y = p[4];
                        }
                        else
                        {
                            inter_x = p[1];
                            inter_y = p[2];
                        }
                        //----------------------hale new first direction: for slab calculation---------------//
                        // calculate the intersection of angle lines and Voronoi
                        linepnt1_x = middleAngleCorner.x;
                        linepnt1_y = middleAngleCorner.y;
                        // vector from middleAngleCorner to largestAngleCorner
                        line_vector_x = largestAngleCorner.x - middleAngleCorner.x;
                        line_vector_y = largestAngleCorner.y - middleAngleCorner.y;
                        // rotate the vector around middleAngleCorner in cw by maxangle degrees				
                        linepnt2_x = petal_slab_inter_x_first;
                        linepnt2_y = petal_slab_inter_y_first;
                        // now calculate the intersection of two lines
                        LineLineIntersection(myCircumcenter.x, myCircumcenter.y, vector_x, vector_y, linepnt1_x, linepnt1_y, linepnt2_x, linepnt2_y, ref line_p);
                        // check if there is a suitable intersection
                        if (line_p[0] > 0.0)
                        {
                            line_inter_x = line_p[1];
                            line_inter_y = line_p[2];
                        }
                        else
                        {
                            // for debugging (to make sure)
                            //printf("1) No intersection between two lines!!!\n");
                            //printf("(%.14f,%.14f) (%.14f,%.14f) (%.14f,%.14f) (%.14f,%.14f)\n",myCircumcenter.x,myCircumcenter.y,vector_x,vector_y,linepnt1_x,linepnt1_y,linepnt2_x,linepnt2_y);
                        }

                        //---------------------------------------------------------------------//
                        /// check if there is a Voronoi vertex between before intersection ///
                        // check if the voronoi vertex is between the intersection and circumcenter
                        PointBetweenPoints(inter_x, inter_y, myCircumcenter.x, myCircumcenter.y,
                                neighborCircumcenter.x, neighborCircumcenter.y, ref voronoiOrInter);

                        /// determine the point to be suggested ///
                        if (p[0] > 0.0)
                        { // there is at least one intersection point
                            // if it is between circumcenter and intersection	
                            // if it returns 1.0 this means we have a voronoi vertex within feasible region
                            if (Math.Abs(voronoiOrInter[0] - 1.0) <= EPS)
                            {
                                //-----------------hale new continues 1------------------//
                                // now check if the line intersection is between cc and voronoi
                                PointBetweenPoints(voronoiOrInter[2], voronoiOrInter[3], myCircumcenter.x, myCircumcenter.y, line_inter_x, line_inter_y, ref line_result);
                                if (Math.Abs(line_result[0] - 1.0) <= EPS && line_p[0] > 0.0)
                                {
                                    // check if we can go further by picking the slab line and petal intersection
                                    // calculate the distance to the smallest angle corner
                                    // check if we create a bad triangle or not
                                    if (((smallestAngleCorner.x - petal_slab_inter_x_first) * (smallestAngleCorner.x - petal_slab_inter_x_first) +
                                    (smallestAngleCorner.y - petal_slab_inter_y_first) * (smallestAngleCorner.y - petal_slab_inter_y_first) >
                                lengthConst * ((smallestAngleCorner.x - line_inter_x) *
                                        (smallestAngleCorner.x - line_inter_x) +
                                        (smallestAngleCorner.y - line_inter_y) *
                                        (smallestAngleCorner.y - line_inter_y)))
                                        && (IsBadTriangleAngle(middleAngleCorner.x, middleAngleCorner.y, largestAngleCorner.x, largestAngleCorner.y, petal_slab_inter_x_first, petal_slab_inter_y_first))
                                        && MinDistanceToNeighbor(petal_slab_inter_x_first, petal_slab_inter_y_first, ref neighborotri) > MinDistanceToNeighbor(line_inter_x, line_inter_y, ref neighborotri))
                                    {
                                        // check the neighbor's vertices also, which one if better
                                        //slab and petal intersection is advised
                                        dxFirstSuggestion = petal_slab_inter_x_first - torg.x;
                                        dyFirstSuggestion = petal_slab_inter_y_first - torg.y;
                                    }
                                    else
                                    { // slab intersection point is further away
                                        if (IsBadTriangleAngle(middleAngleCorner.x, middleAngleCorner.y, largestAngleCorner.x, largestAngleCorner.y, line_inter_x, line_inter_y))
                                        {
                                            // apply perturbation
                                            // find the distance between circumcenter and intersection point
                                            d = Math.Sqrt((line_inter_x - myCircumcenter.x) * (line_inter_x - myCircumcenter.x) +
                                                (line_inter_y - myCircumcenter.y) * (line_inter_y - myCircumcenter.y));
                                            // then find the vector going from intersection point to circumcenter
                                            ax = myCircumcenter.x - line_inter_x;
                                            ay = myCircumcenter.y - line_inter_y;

                                            ax = ax / d;
                                            ay = ay / d;
                                            // now calculate the new intersection point which is perturbated towards the circumcenter
                                            line_inter_x = line_inter_x + ax * pertConst * Math.Sqrt(shortestEdgeDist);
                                            line_inter_y = line_inter_y + ay * pertConst * Math.Sqrt(shortestEdgeDist);
                                            if (IsBadTriangleAngle(middleAngleCorner.x, middleAngleCorner.y, largestAngleCorner.x, largestAngleCorner.y, line_inter_x, line_inter_y))
                                            {
                                                // go back to circumcenter
                                                dxFirstSuggestion = dx;
                                                dyFirstSuggestion = dy;
                                            }
                                            else
                                            {
                                                // intersection point is suggested
                                                dxFirstSuggestion = line_inter_x - torg.x;
                                                dyFirstSuggestion = line_inter_y - torg.y;
                                            }
                                        }
                                        else
                                        {// we are not creating a bad triangle
                                            // slab intersection is advised
                                            dxFirstSuggestion = line_result[2] - torg.x;
                                            dyFirstSuggestion = line_result[3] - torg.y;
                                        }
                                    }
                                    //------------------------------------------------------//
                                }
                                else
                                {
                                    /// NOW APPLY A BREADTH-FIRST SEARCH ON THE VORONOI
                                    if (IsBadTriangleAngle(middleAngleCorner.x, middleAngleCorner.y, largestAngleCorner.x, largestAngleCorner.y, neighborCircumcenter.x, neighborCircumcenter.y))
                                    {
                                        // go back to circumcenter
                                        dxFirstSuggestion = dx;
                                        dyFirstSuggestion = dy;
                                    }
                                    else
                                    {
                                        // we are not creating a bad triangle
                                        // neighbor's circumcenter is suggested
                                        dxFirstSuggestion = voronoiOrInter[2] - torg.x;
                                        dyFirstSuggestion = voronoiOrInter[3] - torg.y;
                                    }
                                }
                            }
                            else
                            { // there is no voronoi vertex between intersection point and circumcenter
                                //-----------------hale new continues 2-----------------//
                                // now check if the line intersection is between cc and intersection point
                                PointBetweenPoints(inter_x, inter_y, myCircumcenter.x, myCircumcenter.y, line_inter_x, line_inter_y, ref line_result);
                                if (Math.Abs(line_result[0] - 1.0) <= EPS && line_p[0] > 0.0)
                                {
                                    // check if we can go further by picking the slab line and petal intersection
                                    // calculate the distance to the smallest angle corner
                                    if (((smallestAngleCorner.x - petal_slab_inter_x_first) * (smallestAngleCorner.x - petal_slab_inter_x_first) +
                                (smallestAngleCorner.y - petal_slab_inter_y_first) * (smallestAngleCorner.y - petal_slab_inter_y_first) >
                                lengthConst * ((smallestAngleCorner.x - line_inter_x) *
                                        (smallestAngleCorner.x - line_inter_x) +
                                        (smallestAngleCorner.y - line_inter_y) *
                                        (smallestAngleCorner.y - line_inter_y)))
                                        && (IsBadTriangleAngle(middleAngleCorner.x, middleAngleCorner.y, largestAngleCorner.x, largestAngleCorner.y, petal_slab_inter_x_first, petal_slab_inter_y_first))
                                        && MinDistanceToNeighbor(petal_slab_inter_x_first, petal_slab_inter_y_first, ref neighborotri) > MinDistanceToNeighbor(line_inter_x, line_inter_y, ref neighborotri))
                                    {
                                        //slab and petal intersection is advised
                                        dxFirstSuggestion = petal_slab_inter_x_first - torg.x;
                                        dyFirstSuggestion = petal_slab_inter_y_first - torg.y;
                                    }
                                    else
                                    { // slab intersection point is further away							
                                        if (IsBadTriangleAngle(largestAngleCorner.x, largestAngleCorner.y, middleAngleCorner.x, middleAngleCorner.y, line_inter_x, line_inter_y))
                                        {
                                            // apply perturbation
                                            // find the distance between circumcenter and intersection point
                                            d = Math.Sqrt((line_inter_x - myCircumcenter.x) * (line_inter_x - myCircumcenter.x) +
                                                (line_inter_y - myCircumcenter.y) * (line_inter_y - myCircumcenter.y));
                                            // then find the vector going from intersection point to circumcenter
                                            ax = myCircumcenter.x - line_inter_x;
                                            ay = myCircumcenter.y - line_inter_y;

                                            ax = ax / d;
                                            ay = ay / d;
                                            // now calculate the new intersection point which is perturbated towards the circumcenter
                                            line_inter_x = line_inter_x + ax * pertConst * Math.Sqrt(shortestEdgeDist);
                                            line_inter_y = line_inter_y + ay * pertConst * Math.Sqrt(shortestEdgeDist);
                                            if (IsBadTriangleAngle(middleAngleCorner.x, middleAngleCorner.y, largestAngleCorner.x, largestAngleCorner.y, line_inter_x, line_inter_y))
                                            {
                                                // go back to circumcenter
                                                dxFirstSuggestion = dx;
                                                dyFirstSuggestion = dy;
                                            }
                                            else
                                            {
                                                // intersection point is suggested
                                                dxFirstSuggestion = line_inter_x - torg.x;
                                                dyFirstSuggestion = line_inter_y - torg.y;
                                            }
                                        }
                                        else
                                        {// we are not creating a bad triangle
                                            // slab intersection is advised
                                            dxFirstSuggestion = line_result[2] - torg.x;
                                            dyFirstSuggestion = line_result[3] - torg.y;
                                        }
                                    }
                                    //------------------------------------------------------//
                                }
                                else
                                {
                                    if (IsBadTriangleAngle(largestAngleCorner.x, largestAngleCorner.y, middleAngleCorner.x, middleAngleCorner.y, inter_x, inter_y))
                                    {
                                        //printf("testtriangle returned false! bad triangle\n");	
                                        // if it is inside feasible region, then insert v2				
                                        // apply perturbation
                                        // find the distance between circumcenter and intersection point
                                        d = Math.Sqrt((inter_x - myCircumcenter.x) * (inter_x - myCircumcenter.x) +
                                            (inter_y - myCircumcenter.y) * (inter_y - myCircumcenter.y));
                                        // then find the vector going from intersection point to circumcenter
                                        ax = myCircumcenter.x - inter_x;
                                        ay = myCircumcenter.y - inter_y;

                                        ax = ax / d;
                                        ay = ay / d;
                                        // now calculate the new intersection point which is perturbated towards the circumcenter
                                        inter_x = inter_x + ax * pertConst * Math.Sqrt(shortestEdgeDist);
                                        inter_y = inter_y + ay * pertConst * Math.Sqrt(shortestEdgeDist);
                                        if (IsBadTriangleAngle(middleAngleCorner.x, middleAngleCorner.y, largestAngleCorner.x, largestAngleCorner.y, inter_x, inter_y))
                                        {
                                            // go back to circumcenter
                                            dxFirstSuggestion = dx;
                                            dyFirstSuggestion = dy;
                                        }
                                        else
                                        {
                                            // intersection point is suggested
                                            dxFirstSuggestion = inter_x - torg.x;
                                            dyFirstSuggestion = inter_y - torg.y;
                                        }
                                    }
                                    else
                                    {
                                        // intersection point is suggested
                                        dxFirstSuggestion = inter_x - torg.x;
                                        dyFirstSuggestion = inter_y - torg.y;
                                    }
                                }
                            }
                            /// if it is an acute triangle, check if it is a good enough location ///
                            // for acute triangle case, we need to check if it is ok to use either of them
                            if ((smallestAngleCorner.x - myCircumcenter.x) * (smallestAngleCorner.x - myCircumcenter.x) +
                                (smallestAngleCorner.y - myCircumcenter.y) * (smallestAngleCorner.y - myCircumcenter.y) >
                                lengthConst * ((smallestAngleCorner.x - (dxFirstSuggestion + torg.x)) *
                                        (smallestAngleCorner.x - (dxFirstSuggestion + torg.x)) +
                                        (smallestAngleCorner.y - (dyFirstSuggestion + torg.y)) *
                                        (smallestAngleCorner.y - (dyFirstSuggestion + torg.y))))
                            {
                                // use circumcenter
                                dxFirstSuggestion = dx;
                                dyFirstSuggestion = dy;

                            }// else we stick to what we have found	
                        }// intersection point

                    }// if it is on the boundary, meaning no neighbor triangle in this direction, try other direction	

                    /// DO THE SAME THING FOR THE OTHER DIRECTION ///
                    /// find the third point of the neighbor triangle  ///
                    neighborNotFound_second = GetNeighborsVertex(badotri, largestAngleCorner.x, largestAngleCorner.y,
                                smallestAngleCorner.x, smallestAngleCorner.y, ref thirdPoint, ref neighborotri);
                    /// find the circumcenter of the neighbor triangle ///
                    dxSecondSuggestion = dx;	// if we cannot find any appropriate suggestion, we use circumcenter
                    dySecondSuggestion = dy;

                    /// choose the correct intersection point ///
                    // calculate middle point of the longest edge(bisector)
                    xMidOfMiddleEdge = (largestAngleCorner.x + smallestAngleCorner.x) / 2.0;
                    yMidOfMiddleEdge = (largestAngleCorner.y + smallestAngleCorner.y) / 2.0;
                    // if there is a neighbor triangle
                    if (!neighborNotFound_second)
                    {
                        neighborvertex_1 = neighborotri.Org();
                        neighborvertex_2 = neighborotri.Dest();
                        neighborvertex_3 = neighborotri.Apex();
                        // now calculate neighbor's circumcenter which is the voronoi site
                        neighborCircumcenter = predicates.FindCircumcenter(neighborvertex_1, neighborvertex_2, neighborvertex_3,
                            ref xi_tmp, ref eta_tmp);

                        /// compute petal and Voronoi edge intersection ///
                        // in order to avoid degenerate cases, we need to do a vector based calculation for line		
                        vector_x = (largestAngleCorner.y - smallestAngleCorner.y);//(-y, x)
                        vector_y = smallestAngleCorner.x - largestAngleCorner.x;
                        vector_x = myCircumcenter.x + vector_x;
                        vector_y = myCircumcenter.y + vector_y;


                        // by intersecting bisectors you will end up with the one you want to walk on
                        // then this line and circle should be intersected
                        CircleLineIntersection(myCircumcenter.x, myCircumcenter.y, vector_x, vector_y,
                                xPetalCtr, yPetalCtr, petalRadius, ref p);

                        // we need to find correct intersection point, since line intersects circle twice
                        // this direction is always ACUTE
                        isCorrect = ChooseCorrectPoint(xMidOfMiddleEdge, yMidOfMiddleEdge, p[3], p[4],
                                    myCircumcenter.x, myCircumcenter.y, false/*(isObtuse+1)%2*/);
                        // make sure which point is the correct one to be considered
                        if (isCorrect)
                        {
                            inter_x = p[3];
                            inter_y = p[4];
                        }
                        else
                        {
                            inter_x = p[1];
                            inter_y = p[2];
                        }
                        //----------------------hale new second direction:for slab calculation---------------//			
                        // calculate the intersection of angle lines and Voronoi
                        linepnt1_x = largestAngleCorner.x;
                        linepnt1_y = largestAngleCorner.y;
                        // vector from largestAngleCorner to middleAngleCorner 
                        line_vector_x = middleAngleCorner.x - largestAngleCorner.x;
                        line_vector_y = middleAngleCorner.y - largestAngleCorner.y;
                        // rotate the vector around largestAngleCorner in ccw by maxangle degrees				
                        linepnt2_x = petal_slab_inter_x_second;
                        linepnt2_y = petal_slab_inter_y_second;
                        // now calculate the intersection of two lines
                        LineLineIntersection(myCircumcenter.x, myCircumcenter.y, vector_x, vector_y, linepnt1_x, linepnt1_y, linepnt2_x, linepnt2_y, ref line_p);
                        // check if there is a suitable intersection
                        if (line_p[0] > 0.0)
                        {
                            line_inter_x = line_p[1];
                            line_inter_y = line_p[2];
                        }
                        else
                        {
                            // for debugging (to make sure)
                            //printf("1) No intersection between two lines!!!\n");
                            //printf("(%.14f,%.14f) (%.14f,%.14f) (%.14f,%.14f) (%.14f,%.14f)\n",myCircumcenter.x,myCircumcenter.y,vector_x,vector_y,linepnt1_x,linepnt1_y,linepnt2_x,linepnt2_y);
                        }
                        //---------------------------------------------------------------------//
                        /// check if there is a Voronoi vertex between before intersection ///
                        // check if the voronoi vertex is between the intersection and circumcenter
                        PointBetweenPoints(inter_x, inter_y, myCircumcenter.x, myCircumcenter.y,
                                neighborCircumcenter.x, neighborCircumcenter.y, ref voronoiOrInter);
                        /// determine the point to be suggested ///
                        if (p[0] > 0.0)
                        { // there is at least one intersection point				
                            // if it is between circumcenter and intersection	
                            // if it returns 1.0 this means we have a voronoi vertex within feasible region
                            if (Math.Abs(voronoiOrInter[0] - 1.0) <= EPS)
                            {
                                //-----------------hale new continues 1------------------//
                                // now check if the line intersection is between cc and voronoi
                                PointBetweenPoints(voronoiOrInter[2], voronoiOrInter[3], myCircumcenter.x, myCircumcenter.y, line_inter_x, line_inter_y, ref line_result);
                                if (Math.Abs(line_result[0] - 1.0) <= EPS && line_p[0] > 0.0)
                                {
                                    // check if we can go further by picking the slab line and petal intersection
                                    // calculate the distance to the smallest angle corner
                                    // 						
                                    if (((smallestAngleCorner.x - petal_slab_inter_x_second) * (smallestAngleCorner.x - petal_slab_inter_x_second) +
                                (smallestAngleCorner.y - petal_slab_inter_y_second) * (smallestAngleCorner.y - petal_slab_inter_y_second) >
                                lengthConst * ((smallestAngleCorner.x - line_inter_x) *
                                        (smallestAngleCorner.x - line_inter_x) +
                                        (smallestAngleCorner.y - line_inter_y) *
                                        (smallestAngleCorner.y - line_inter_y)))
                                        && (IsBadTriangleAngle(middleAngleCorner.x, middleAngleCorner.y, largestAngleCorner.x, largestAngleCorner.y, petal_slab_inter_x_second, petal_slab_inter_y_second))
                                        && MinDistanceToNeighbor(petal_slab_inter_x_second, petal_slab_inter_y_second, ref neighborotri) > MinDistanceToNeighbor(line_inter_x, line_inter_y, ref neighborotri))
                                    {
                                        // slab and petal intersection is advised
                                        dxSecondSuggestion = petal_slab_inter_x_second - torg.x;
                                        dySecondSuggestion = petal_slab_inter_y_second - torg.y;
                                    }
                                    else
                                    { // slab intersection point is further away	
                                        if (IsBadTriangleAngle(middleAngleCorner.x, middleAngleCorner.y, largestAngleCorner.x, largestAngleCorner.y, line_inter_x, line_inter_y))
                                        {
                                            // apply perturbation
                                            // find the distance between circumcenter and intersection point
                                            d = Math.Sqrt((line_inter_x - myCircumcenter.x) * (line_inter_x - myCircumcenter.x) +
                                                (line_inter_y - myCircumcenter.y) * (line_inter_y - myCircumcenter.y));
                                            // then find the vector going from intersection point to circumcenter
                                            ax = myCircumcenter.x - line_inter_x;
                                            ay = myCircumcenter.y - line_inter_y;

                                            ax = ax / d;
                                            ay = ay / d;
                                            // now calculate the new intersection point which is perturbated towards the circumcenter
                                            line_inter_x = line_inter_x + ax * pertConst * Math.Sqrt(shortestEdgeDist);
                                            line_inter_y = line_inter_y + ay * pertConst * Math.Sqrt(shortestEdgeDist);
                                            if (IsBadTriangleAngle(middleAngleCorner.x, middleAngleCorner.y, largestAngleCorner.x, largestAngleCorner.y, line_inter_x, line_inter_y))
                                            {
                                                // go back to circumcenter
                                                dxSecondSuggestion = dx;
                                                dySecondSuggestion = dy;
                                            }
                                            else
                                            {
                                                // intersection point is suggested
                                                dxSecondSuggestion = line_inter_x - torg.x;
                                                dySecondSuggestion = line_inter_y - torg.y;

                                            }
                                        }
                                        else
                                        {// we are not creating a bad triangle
                                            // slab intersection is advised
                                            dxSecondSuggestion = line_result[2] - torg.x;
                                            dySecondSuggestion = line_result[3] - torg.y;
                                        }
                                    }
                                    //------------------------------------------------------//
                                }
                                else
                                {
                                    if (IsBadTriangleAngle(middleAngleCorner.x, middleAngleCorner.y, largestAngleCorner.x, largestAngleCorner.y, neighborCircumcenter.x, neighborCircumcenter.y))
                                    {
                                        // go back to circumcenter
                                        dxSecondSuggestion = dx;
                                        dySecondSuggestion = dy;
                                    }
                                    else
                                    { // we are not creating a bad triangle
                                        // neighbor's circumcenter is suggested
                                        dxSecondSuggestion = voronoiOrInter[2] - torg.x;
                                        dySecondSuggestion = voronoiOrInter[3] - torg.y;
                                    }
                                }
                            }
                            else
                            { // there is no voronoi vertex between intersection point and circumcenter
                                //-----------------hale new continues 2-----------------//
                                // now check if the line intersection is between cc and intersection point
                                PointBetweenPoints(inter_x, inter_y, myCircumcenter.x, myCircumcenter.y, line_inter_x, line_inter_y, ref line_result);
                                if (Math.Abs(line_result[0] - 1.0) <= EPS && line_p[0] > 0.0)
                                {
                                    // check if we can go further by picking the slab line and petal intersection
                                    // calculate the distance to the smallest angle corner
                                    if (((smallestAngleCorner.x - petal_slab_inter_x_second) * (smallestAngleCorner.x - petal_slab_inter_x_second) +
                                (smallestAngleCorner.y - petal_slab_inter_y_second) * (smallestAngleCorner.y - petal_slab_inter_y_second) >
                                lengthConst * ((smallestAngleCorner.x - line_inter_x) *
                                        (smallestAngleCorner.x - line_inter_x) +
                                        (smallestAngleCorner.y - line_inter_y) *
                                        (smallestAngleCorner.y - line_inter_y)))
                                        && (IsBadTriangleAngle(middleAngleCorner.x, middleAngleCorner.y, largestAngleCorner.x, largestAngleCorner.y, petal_slab_inter_x_second, petal_slab_inter_y_second))
                                        && MinDistanceToNeighbor(petal_slab_inter_x_second, petal_slab_inter_y_second, ref neighborotri) > MinDistanceToNeighbor(line_inter_x, line_inter_y, ref neighborotri))
                                    {
                                        // slab and petal intersection is advised
                                        dxSecondSuggestion = petal_slab_inter_x_second - torg.x;
                                        dySecondSuggestion = petal_slab_inter_y_second - torg.y;
                                    }
                                    else
                                    { // slab intersection point is further away							;
                                        if (IsBadTriangleAngle(largestAngleCorner.x, largestAngleCorner.y, middleAngleCorner.x, middleAngleCorner.y, line_inter_x, line_inter_y))
                                        {
                                            // apply perturbation
                                            // find the distance between circumcenter and intersection point
                                            d = Math.Sqrt((line_inter_x - myCircumcenter.x) * (line_inter_x - myCircumcenter.x) +
                                                (line_inter_y - myCircumcenter.y) * (line_inter_y - myCircumcenter.y));
                                            // then find the vector going from intersection point to circumcenter
                                            ax = myCircumcenter.x - line_inter_x;
                                            ay = myCircumcenter.y - line_inter_y;

                                            ax = ax / d;
                                            ay = ay / d;
                                            // now calculate the new intersection point which is perturbated towards the circumcenter
                                            line_inter_x = line_inter_x + ax * pertConst * Math.Sqrt(shortestEdgeDist);
                                            line_inter_y = line_inter_y + ay * pertConst * Math.Sqrt(shortestEdgeDist);
                                            if (IsBadTriangleAngle(middleAngleCorner.x, middleAngleCorner.y, largestAngleCorner.x, largestAngleCorner.y, line_inter_x, line_inter_y))
                                            {
                                                // go back to circumcenter
                                                dxSecondSuggestion = dx;
                                                dySecondSuggestion = dy;
                                            }
                                            else
                                            {
                                                // intersection point is suggested
                                                dxSecondSuggestion = line_inter_x - torg.x;
                                                dySecondSuggestion = line_inter_y - torg.y;
                                            }
                                        }
                                        else
                                        {
                                            // we are not creating a bad triangle
                                            // slab intersection is advised
                                            dxSecondSuggestion = line_result[2] - torg.x;
                                            dySecondSuggestion = line_result[3] - torg.y;
                                        }
                                    }
                                    //------------------------------------------------------//
                                }
                                else
                                {
                                    if (IsBadTriangleAngle(middleAngleCorner.x, middleAngleCorner.y, largestAngleCorner.x, largestAngleCorner.y, inter_x, inter_y))
                                    {
                                        // if it is inside feasible region, then insert v2				
                                        // apply perturbation
                                        // find the distance between circumcenter and intersection point
                                        d = Math.Sqrt((inter_x - myCircumcenter.x) * (inter_x - myCircumcenter.x) +
                                            (inter_y - myCircumcenter.y) * (inter_y - myCircumcenter.y));
                                        // then find the vector going from intersection point to circumcenter
                                        ax = myCircumcenter.x - inter_x;
                                        ay = myCircumcenter.y - inter_y;

                                        ax = ax / d;
                                        ay = ay / d;
                                        // now calculate the new intersection point which is perturbated towards the circumcenter
                                        inter_x = inter_x + ax * pertConst * Math.Sqrt(shortestEdgeDist);
                                        inter_y = inter_y + ay * pertConst * Math.Sqrt(shortestEdgeDist);
                                        if (IsBadTriangleAngle(middleAngleCorner.x, middleAngleCorner.y, largestAngleCorner.x, largestAngleCorner.y, inter_x, inter_y))
                                        {
                                            // go back to circumcenter
                                            dxSecondSuggestion = dx;
                                            dySecondSuggestion = dy;
                                        }
                                        else
                                        {
                                            // intersection point is suggested
                                            dxSecondSuggestion = inter_x - torg.x;
                                            dySecondSuggestion = inter_y - torg.y;
                                        }
                                    }
                                    else
                                    {
                                        // intersection point is suggested
                                        dxSecondSuggestion = inter_x - torg.x;
                                        dySecondSuggestion = inter_y - torg.y;
                                    }
                                }
                            }

                            /// if it is an acute triangle, check if it is a good enough location ///
                            // for acute triangle case, we need to check if it is ok to use either of them
                            if ((smallestAngleCorner.x - myCircumcenter.x) * (smallestAngleCorner.x - myCircumcenter.x) +
                                (smallestAngleCorner.y - myCircumcenter.y) * (smallestAngleCorner.y - myCircumcenter.y) >
                                lengthConst * ((smallestAngleCorner.x - (dxSecondSuggestion + torg.x)) *
                                        (smallestAngleCorner.x - (dxSecondSuggestion + torg.x)) +
                                        (smallestAngleCorner.y - (dySecondSuggestion + torg.y)) *
                                        (smallestAngleCorner.y - (dySecondSuggestion + torg.y))))
                            {
                                // use circumcenter
                                dxSecondSuggestion = dx;
                                dySecondSuggestion = dy;

                            }// else we stick on what we have found	
                        }
                    }// if it is on the boundary, meaning no neighbor triangle in this direction, the other direction might be ok	
                    if (isObtuse)
                    {
                        if (neighborNotFound_first && neighborNotFound_second)
                        {
                            //obtuse: check if the other direction works	
                            if (justAcute * ((smallestAngleCorner.x - (xMidOfMiddleEdge)) *
                                (smallestAngleCorner.x - (xMidOfMiddleEdge)) +
                                (smallestAngleCorner.y - (yMidOfMiddleEdge)) *
                                (smallestAngleCorner.y - (yMidOfMiddleEdge))) >
                                (smallestAngleCorner.x - (xMidOfLongestEdge)) *
                                (smallestAngleCorner.x - (xMidOfLongestEdge)) +
                                (smallestAngleCorner.y - (yMidOfLongestEdge)) *
                                (smallestAngleCorner.y - (yMidOfLongestEdge)))
                            {
                                dx = dxSecondSuggestion;
                                dy = dySecondSuggestion;
                            }
                            else
                            {
                                dx = dxFirstSuggestion;
                                dy = dyFirstSuggestion;
                            }
                        }
                        else if (neighborNotFound_first)
                        {
                            //obtuse: check if the other direction works	
                            if (justAcute * ((smallestAngleCorner.x - (dxSecondSuggestion + torg.x)) *
                                    (smallestAngleCorner.x - (dxSecondSuggestion + torg.x)) +
                                    (smallestAngleCorner.y - (dySecondSuggestion + torg.y)) *
                                    (smallestAngleCorner.y - (dySecondSuggestion + torg.y))) >
                                    (smallestAngleCorner.x - (xMidOfLongestEdge)) *
                                    (smallestAngleCorner.x - (xMidOfLongestEdge)) +
                                    (smallestAngleCorner.y - (yMidOfLongestEdge)) *
                                    (smallestAngleCorner.y - (yMidOfLongestEdge)))
                            {
                                dx = dxSecondSuggestion;
                                dy = dySecondSuggestion;
                            }
                            else
                            {
                                dx = dxFirstSuggestion;
                                dy = dyFirstSuggestion;
                            }
                        }
                        else if (neighborNotFound_second)
                        {
                            //obtuse: check if the other direction works	
                            if (justAcute * ((smallestAngleCorner.x - (xMidOfMiddleEdge)) *
                                    (smallestAngleCorner.x - (xMidOfMiddleEdge)) +
                                    (smallestAngleCorner.y - (yMidOfMiddleEdge)) *
                                    (smallestAngleCorner.y - (yMidOfMiddleEdge))) >
                                    (smallestAngleCorner.x - (dxFirstSuggestion + torg.x)) *
                                    (smallestAngleCorner.x - (dxFirstSuggestion + torg.x)) +
                                    (smallestAngleCorner.y - (dyFirstSuggestion + torg.y)) *
                                    (smallestAngleCorner.y - (dyFirstSuggestion + torg.y)))
                            {
                                dx = dxSecondSuggestion;
                                dy = dySecondSuggestion;
                            }
                            else
                            {
                                dx = dxFirstSuggestion;
                                dy = dyFirstSuggestion;
                            }
                        }
                        else
                        {
                            //obtuse: check if the other direction works	
                            if (justAcute * ((smallestAngleCorner.x - (dxSecondSuggestion + torg.x)) *
                                (smallestAngleCorner.x - (dxSecondSuggestion + torg.x)) +
                                (smallestAngleCorner.y - (dySecondSuggestion + torg.y)) *
                                (smallestAngleCorner.y - (dySecondSuggestion + torg.y))) >
                                (smallestAngleCorner.x - (dxFirstSuggestion + torg.x)) *
                                (smallestAngleCorner.x - (dxFirstSuggestion + torg.x)) +
                                (smallestAngleCorner.y - (dyFirstSuggestion + torg.y)) *
                                (smallestAngleCorner.y - (dyFirstSuggestion + torg.y)))
                            {
                                dx = dxSecondSuggestion;
                                dy = dySecondSuggestion;
                            }
                            else
                            {
                                dx = dxFirstSuggestion;
                                dy = dyFirstSuggestion;
                            }
                        }
                    }
                    else
                    { // acute : consider other direction
                        if (neighborNotFound_first && neighborNotFound_second)
                        {
                            //obtuse: check if the other direction works	
                            if (justAcute * ((smallestAngleCorner.x - (xMidOfMiddleEdge)) *
                                (smallestAngleCorner.x - (xMidOfMiddleEdge)) +
                                (smallestAngleCorner.y - (yMidOfMiddleEdge)) *
                                (smallestAngleCorner.y - (yMidOfMiddleEdge))) >
                                (smallestAngleCorner.x - (xMidOfLongestEdge)) *
                                (smallestAngleCorner.x - (xMidOfLongestEdge)) +
                                (smallestAngleCorner.y - (yMidOfLongestEdge)) *
                                (smallestAngleCorner.y - (yMidOfLongestEdge)))
                            {
                                dx = dxSecondSuggestion;
                                dy = dySecondSuggestion;
                            }
                            else
                            {
                                dx = dxFirstSuggestion;
                                dy = dyFirstSuggestion;
                            }
                        }
                        else if (neighborNotFound_first)
                        {
                            //obtuse: check if the other direction works	
                            if (justAcute * ((smallestAngleCorner.x - (dxSecondSuggestion + torg.x)) *
                                    (smallestAngleCorner.x - (dxSecondSuggestion + torg.x)) +
                                    (smallestAngleCorner.y - (dySecondSuggestion + torg.y)) *
                                    (smallestAngleCorner.y - (dySecondSuggestion + torg.y))) >
                                    (smallestAngleCorner.x - (xMidOfLongestEdge)) *
                                    (smallestAngleCorner.x - (xMidOfLongestEdge)) +
                                    (smallestAngleCorner.y - (yMidOfLongestEdge)) *
                                    (smallestAngleCorner.y - (yMidOfLongestEdge)))
                            {
                                dx = dxSecondSuggestion;
                                dy = dySecondSuggestion;
                            }
                            else
                            {
                                dx = dxFirstSuggestion;
                                dy = dyFirstSuggestion;
                            }
                        }
                        else if (neighborNotFound_second)
                        {
                            //obtuse: check if the other direction works	
                            if (justAcute * ((smallestAngleCorner.x - (xMidOfMiddleEdge)) *
                                    (smallestAngleCorner.x - (xMidOfMiddleEdge)) +
                                    (smallestAngleCorner.y - (yMidOfMiddleEdge)) *
                                    (smallestAngleCorner.y - (yMidOfMiddleEdge))) >
                                    (smallestAngleCorner.x - (dxFirstSuggestion + torg.x)) *
                                    (smallestAngleCorner.x - (dxFirstSuggestion + torg.x)) +
                                    (smallestAngleCorner.y - (dyFirstSuggestion + torg.y)) *
                                    (smallestAngleCorner.y - (dyFirstSuggestion + torg.y)))
                            {
                                dx = dxSecondSuggestion;
                                dy = dySecondSuggestion;
                            }
                            else
                            {
                                dx = dxFirstSuggestion;
                                dy = dyFirstSuggestion;
                            }
                        }
                        else
                        {
                            //obtuse: check if the other direction works	
                            if (justAcute * ((smallestAngleCorner.x - (dxSecondSuggestion + torg.x)) *
                                (smallestAngleCorner.x - (dxSecondSuggestion + torg.x)) +
                                (smallestAngleCorner.y - (dySecondSuggestion + torg.y)) *
                                (smallestAngleCorner.y - (dySecondSuggestion + torg.y))) >
                                (smallestAngleCorner.x - (dxFirstSuggestion + torg.x)) *
                                (smallestAngleCorner.x - (dxFirstSuggestion + torg.x)) +
                                (smallestAngleCorner.y - (dyFirstSuggestion + torg.y)) *
                                (smallestAngleCorner.y - (dyFirstSuggestion + torg.y)))
                            {
                                dx = dxSecondSuggestion;
                                dy = dySecondSuggestion;
                            }
                            else
                            {
                                dx = dxFirstSuggestion;
                                dy = dyFirstSuggestion;
                            }
                        }

                    }// end if obtuse
                }// end of relocation				 
            }// end of almostGood	

            Point circumcenter = new Point();

            if (relocated <= 0)
            {
                circumcenter.x = torg.x + dx;
                circumcenter.y = torg.y + dy;
            }
            else
            {
                circumcenter.x = origin_x + dx;
                circumcenter.y = origin_y + dy;
            }
            xi = (yao * dx - xao * dy) * (2.0 * denominator);
            eta = (xdo * dy - ydo * dx) * (2.0 * denominator);

            return circumcenter;
        }

        /// <summary>
        /// Given square of edge lengths of a triangle,
        // determine its orientation
        /// </summary>
        /// <param name="aodist"></param>
        /// <param name="dadist"></param>
        /// <param name="dodist"></param>
        /// <returns>Returns a number indicating an orientation.</returns>
        private int LongestShortestEdge(double aodist, double dadist, double dodist)
        {
            // 123: shortest: aodist	// 213: shortest: dadist	// 312: shortest: dodist	
            //	middle: dadist 		//	middle: aodist 		//	middle: aodist 
            //	longest: dodist		//	longest: dodist		//	longest: dadist
            // 132: shortest: aodist 	// 231: shortest: dadist 	// 321: shortest: dodist 
            //	middle: dodist 		//	middle: dodist 		//	middle: dadist 
            //	longest: dadist		//	longest: aodist		//	longest: aodist

            int max = 0, min = 0, mid = 0, minMidMax;
            if (dodist < aodist && dodist < dadist)
            {
                min = 3; // apex is the smallest angle, dodist is the longest edge
                if (aodist < dadist)
                {
                    max = 2; // dadist is the longest edge 
                    mid = 1; // aodist is the middle longest edge
                }
                else
                {
                    max = 1; // aodist is the longest edge 
                    mid = 2; // dadist is the middle longest edge
                }
            }
            else if (aodist < dadist)
            {
                min = 1; // dest is the smallest angle, aodist is the biggest edge
                if (dodist < dadist)
                {
                    max = 2; // dadist is the longest edge 
                    mid = 3; // dodist is the middle longest edge
                }
                else
                {
                    max = 3; // dodist is the longest edge 
                    mid = 2; // dadist is the middle longest edge
                }
            }
            else
            {
                min = 2; // origin is the smallest angle, dadist is the biggest edge
                if (aodist < dodist)
                {
                    max = 3; // dodist is the longest edge 
                    mid = 1; // aodist is the middle longest edge
                }
                else
                {
                    max = 1; // aodist is the longest edge 
                    mid = 3; // dodist is the middle longest edge
                }
            }
            minMidMax = min * 100 + mid * 10 + max;
            // HANDLE ISOSCELES TRIANGLE CASE
            return minMidMax;
        }

        /// <summary>
        /// Checks if smothing is possible for a given bad triangle.
        /// </summary>
        /// <param name="badotri"></param>
        /// <param name="torg"></param>
        /// <param name="tdest"></param>
        /// <param name="tapex"></param>
        /// <param name="newloc">The new location for the point, if somothing is possible.</param>
        /// <returns>Returns 1, 2 or 3 if smoothing will work, 0 otherwise.</returns>
        private int DoSmoothing(Otri badotri, Vertex torg, Vertex tdest, Vertex tapex,
            ref double[] newloc)
        {

            int numpoints_p = 0;// keeps the number of points in a star of point p, q, r
            int numpoints_q = 0;
            int numpoints_r = 0;
            //int i;	
            double[] possibilities = new double[6];//there can be more than one possibilities
            int num_pos = 0; // number of possibilities
            int flag1 = 0, flag2 = 0, flag3 = 0;
            bool newLocFound = false;

            //vertex v1, v2, v3;	// for ccw test
            //double p1[2], p2[2], p3[2];
            //double temp[2];

            //********************* TRY TO RELOCATE POINT "p" ***************

            // get the surrounding points of p, so this gives us the triangles
            numpoints_p = GetStarPoints(badotri, torg, tdest, tapex, 1, ref points_p);
            // check if the points in counterclockwise order
            // 	p1[0] = points_p[0];  p1[1] = points_p[1];
            // 	p2[0] = points_p[2];  p2[1] = points_p[3];
            // 	p3[0] = points_p[4];  p3[1] = points_p[5];
            // 	v1 = (vertex)p1; v2 = (vertex)p2; v3 = (vertex)p3; 
            // 	if(counterclockwise(m,b,v1,v2,v3) < 0){
            // 		// reverse the order to ccw
            // 		for(i = 0; i < numpoints_p/2; i++){
            // 			temp[0] = points_p[2*i];	
            // 			temp[1] = points_p[2*i+1];
            // 			points_p[2*i] = points_p[2*(numpoints_p-1)-2*i];
            // 			points_p[2*i+1] = points_p[2*(numpoints_p-1)+1-2*i];
            // 			points_p[2*(numpoints_p-1)-2*i] = temp[0];
            // 			points_p[2*(numpoints_p-1)+1-2*i] = temp[1];
            // 		}
            // 	}
            // 	m.counterclockcount--;
            // INTERSECTION OF PETALS
            // first check whether the star angles are appropriate for relocation
            if (torg.type == VertexType.FreeVertex && numpoints_p != 0 && ValidPolygonAngles(numpoints_p, points_p))
            {
                //newLocFound = getPetalIntersection(m, b, numpoints_p, points_p, newloc);
                //newLocFound = getPetalIntersectionBruteForce(m, b,numpoints_p, points_p, newloc,torg[0],torg[1]);
                if (behavior.MaxAngle == 0.0)
                {
                    newLocFound = GetWedgeIntersectionWithoutMaxAngle(numpoints_p, points_p, ref newloc);
                }
                else
                {
                    newLocFound = GetWedgeIntersection(numpoints_p, points_p, ref newloc);
                }
                //printf("call petal intersection for p\n");
                // make sure the relocated point is a free vertex	
                if (newLocFound)
                {
                    possibilities[0] = newloc[0];// something found
                    possibilities[1] = newloc[1];
                    num_pos++;// increase the number of possibilities
                    flag1 = 1;
                }
            }

            //********************* TRY TO RELOCATE POINT "q" ***************		

            // get the surrounding points of q, so this gives us the triangles
            numpoints_q = GetStarPoints(badotri, torg, tdest, tapex, 2, ref points_q);
            // 	// check if the points in counterclockwise order
            // 	v1[0] = points_q[0];  v1[1] = points_q[1];
            // 	v2[0] = points_q[2];  v2[1] = points_q[3];
            // 	v3[0] = points_q[4];  v3[1] = points_q[5];
            // 	if(counterclockwise(m,b,v1,v2,v3) < 0){
            // 		// reverse the order to ccw
            // 		for(i = 0; i < numpoints_q/2; i++){
            // 			temp[0] = points_q[2*i];	
            // 			temp[1] = points_q[2*i+1];
            // 			points_q[2*i] = points_q[2*(numpoints_q-1)-2*i];
            // 			points_q[2*i+1] = points_q[2*(numpoints_q-1)+1-2*i];
            // 			points_q[2*(numpoints_q-1)-2*i] = temp[0];
            // 			points_q[2*(numpoints_q-1)+1-2*i] = temp[1];
            // 		}
            // 	}
            // 	m.counterclockcount--;
            // INTERSECTION OF PETALS
            // first check whether the star angles are appropriate for relocation
            if (tdest.type == VertexType.FreeVertex && numpoints_q != 0 && ValidPolygonAngles(numpoints_q, points_q))
            {
                //newLocFound = getPetalIntersection(m, b,numpoints_q, points_q, newloc);
                //newLocFound = getPetalIntersectionBruteForce(m, b,numpoints_q, points_q, newloc,tapex[0],tapex[1]);
                if (behavior.MaxAngle == 0.0)
                {
                    newLocFound = GetWedgeIntersectionWithoutMaxAngle(numpoints_q, points_q, ref newloc);
                }
                else
                {
                    newLocFound = GetWedgeIntersection(numpoints_q, points_q, ref newloc);
                }
                //printf("call petal intersection for q\n");	

                // make sure the relocated point is a free vertex	
                if (newLocFound)
                {
                    possibilities[2] = newloc[0];// something found
                    possibilities[3] = newloc[1];
                    num_pos++;// increase the number of possibilities
                    flag2 = 2;
                }
            }


            //********************* TRY TO RELOCATE POINT "q" ***************		
            // get the surrounding points of r, so this gives us the triangles
            numpoints_r = GetStarPoints(badotri, torg, tdest, tapex, 3, ref points_r);
            // check if the points in counterclockwise order
            // 	v1[0] = points_r[0];  v1[1] = points_r[1];
            // 	v2[0] = points_r[2];  v2[1] = points_r[3];
            // 	v3[0] = points_r[4];  v3[1] = points_r[5];
            // 	if(counterclockwise(m,b,v1,v2,v3) < 0){
            // 		// reverse the order to ccw
            // 		for(i = 0; i < numpoints_r/2; i++){
            // 			temp[0] = points_r[2*i];	
            // 			temp[1] = points_r[2*i+1];
            // 			points_r[2*i] = points_r[2*(numpoints_r-1)-2*i];
            // 			points_r[2*i+1] = points_r[2*(numpoints_r-1)+1-2*i];
            // 			points_r[2*(numpoints_r-1)-2*i] = temp[0];
            // 			points_r[2*(numpoints_r-1)+1-2*i] = temp[1];
            // 		}
            // 	}
            // 	m.counterclockcount--;
            // INTERSECTION OF PETALS
            // first check whether the star angles are appropriate for relocation
            if (tapex.type == VertexType.FreeVertex && numpoints_r != 0 && ValidPolygonAngles(numpoints_r, points_r))
            {
                //newLocFound = getPetalIntersection(m, b,numpoints_r, points_r, newloc);
                //newLocFound = getPetalIntersectionBruteForce(m, b,numpoints_r, points_r, newloc,tdest[0],tdest[1]);
                if (behavior.MaxAngle == 0.0)
                {
                    newLocFound = GetWedgeIntersectionWithoutMaxAngle(numpoints_r, points_r, ref newloc);
                }
                else
                {
                    newLocFound = GetWedgeIntersection(numpoints_r, points_r, ref newloc);
                }

                //printf("call petal intersection for r\n");


                // make sure the relocated point is a free vertex	
                if (newLocFound)
                {
                    possibilities[4] = newloc[0];// something found
                    possibilities[5] = newloc[1];
                    num_pos++;// increase the number of possibilities
                    flag3 = 3;
                }
            }
            //printf("numpossibilities %d\n",num_pos);
            //////////// AFTER FINISH CHECKING EVERY POSSIBILITY, CHOOSE ANY OF THE AVAILABLE ONE //////////////////////	
            if (num_pos > 0)
            {
                if (flag1 > 0)
                { // suggest to relocate origin
                    newloc[0] = possibilities[0];
                    newloc[1] = possibilities[1];
                    return flag1;

                }
                else
                {
                    if (flag2 > 0)
                    { // suggest to relocate apex
                        newloc[0] = possibilities[2];
                        newloc[1] = possibilities[3];
                        return flag2;

                    }
                    else
                    {// suggest to relocate destination
                        if (flag3 > 0)
                        {
                            newloc[0] = possibilities[4];
                            newloc[1] = possibilities[5];
                            return flag3;

                        }
                    }
                }
            }

            return 0;// could not find any good relocation
        }

        /// <summary>
        /// Finds the star of a given point.
        /// </summary>
        /// <param name="badotri"></param>
        /// <param name="p"></param>
        /// <param name="q"></param>
        /// <param name="r"></param>
        /// <param name="whichPoint"></param>
        /// <param name="points">List of points on the star of the given point.</param>
        /// <returns>Number of points on the star of the given point.</returns>
        private int GetStarPoints(Otri badotri, Vertex p, Vertex q, Vertex r,
                    int whichPoint, ref double[] points)
        {

            Otri neighotri = default(Otri);  // for return value of the function
            Otri tempotri;   // for temporary usage
            double first_x = 0, first_y = 0;	  // keeps the first point to be considered
            double second_x = 0, second_y = 0;  // for determining the edge we will begin
            double third_x = 0, third_y = 0;	  // termination
            double[] returnPoint = new double[2];	  // for keeping the returned point	
            int numvertices = 0;	  // for keeping number of surrounding vertices

            // first determine which point to be used to find its neighbor triangles
            switch (whichPoint)
            {
                case 1:
                    first_x = p.x;	// point at the center
                    first_y = p.y;
                    second_x = r.x; // second vertex of first edge to consider
                    second_y = r.y;
                    third_x = q.x;  // for terminating the search
                    third_y = q.y;
                    break;
                case 2:
                    first_x = q.x;  // point at the center
                    first_y = q.y;
                    second_x = p.x; // second vertex of first edge to consider
                    second_y = p.y;
                    third_x = r.x;	// for terminating the search
                    third_y = r.y;
                    break;
                case 3:
                    first_x = r.x;	// point at the center
                    first_y = r.y;
                    second_x = q.x; // second vertex of first edge to consider
                    second_y = q.y;
                    third_x = p.x;	// for terminating the search
                    third_y = p.y;
                    break;
            }
            tempotri = badotri;
            // add first point as the end of first edge
            points[numvertices] = second_x;
            numvertices++;
            points[numvertices] = second_y;
            numvertices++;
            // assign as dummy value
            returnPoint[0] = second_x; returnPoint[1] = second_y;
            // until we reach the third point of the beginning triangle	
            do
            {
                // find the neighbor's third point where it is incident to given edge
                if (!GetNeighborsVertex(tempotri, first_x, first_y, second_x, second_y, ref returnPoint, ref neighotri))
                {
                    // go to next triangle
                    tempotri = neighotri;
                    // now the second point is the neighbor's third vertex			
                    second_x = returnPoint[0];
                    second_y = returnPoint[1];
                    // add a new point to the list of surrounding points
                    points[numvertices] = returnPoint[0];
                    numvertices++;
                    points[numvertices] = returnPoint[1];
                    numvertices++;
                }
                else
                {
                    numvertices = 0;
                    break;
                }

            } while (!((Math.Abs(returnPoint[0] - third_x) <= EPS) &&
                     (Math.Abs(returnPoint[1] - third_y) <= EPS)));
            return numvertices / 2;

        }

        /// <summary>
        /// Gets a neighbours vertex.
        /// </summary>
        /// <param name="badotri"></param>
        /// <param name="first_x"></param>
        /// <param name="first_y"></param>
        /// <param name="second_x"></param>
        /// <param name="second_y"></param>
        /// <param name="thirdpoint">Neighbor's third vertex incident to given edge.</param>
        /// <param name="neighotri">Pointer for the neighbor triangle.</param>
        /// <returns>Returns true if vertex was found.</returns>
        private bool GetNeighborsVertex(Otri badotri,
                        double first_x, double first_y,
                        double second_x, double second_y,
                        ref double[] thirdpoint, ref Otri neighotri)
        {

            Otri neighbor = default(Otri); // keeps the neighbor triangles
            bool notFound = false;	// boolean variable if we can find that neighbor or not

            // for keeping the vertices of the neighbor triangle
            Vertex neighborvertex_1 = null;
            Vertex neighborvertex_2 = null;
            Vertex neighborvertex_3 = null;

            // used for finding neighbor triangle
            int firstVertexMatched = 0, secondVertexMatched = 0;	// to find the correct neighbor
            //triangle ptr;             // Temporary variable used by sym()
            //int i;	// index variable	
            // find neighbors
            // Check each of the triangle's three neighbors to find the correct one
            for (badotri.orient = 0; badotri.orient < 3; badotri.orient++)
            {
                // Find the neighbor.
                badotri.Sym(ref neighbor);
                // check if it is the one we are looking for by checking the corners			
                // first check if the neighbor is nonexistent, since it can be on the border
                if (neighbor.tri.id != Mesh.DUMMY)
                {
                    // then check if two wanted corners are also in this triangle
                    // take the vertices of the candidate neighbor		
                    neighborvertex_1 = neighbor.Org();
                    neighborvertex_2 = neighbor.Dest();
                    neighborvertex_3 = neighbor.Apex();

                    // check if it is really a triangle
                    if ((neighborvertex_1.x == neighborvertex_2.x && neighborvertex_1.y == neighborvertex_2.y)
                     || (neighborvertex_2.x == neighborvertex_3.x && neighborvertex_2.y == neighborvertex_3.y)
                     || (neighborvertex_1.x == neighborvertex_3.x && neighborvertex_1.y == neighborvertex_3.y))
                    {
                        //printf("Two vertices are the same!!!!!!!\n");
                    }
                    else
                    {
                        // begin searching for the correct neighbor triangle
                        firstVertexMatched = 0;
                        if ((Math.Abs(first_x - neighborvertex_1.x) < EPS) &&
                             (Math.Abs(first_y - neighborvertex_1.y) < EPS))
                        {
                            firstVertexMatched = 11; // neighbor's 1st vertex is matched to first vertex

                        }
                        else if ((Math.Abs(first_x - neighborvertex_2.x) < EPS) &&
                               (Math.Abs(first_y - neighborvertex_2.y) < EPS))
                        {
                            firstVertexMatched = 12; // neighbor's 2nd vertex is matched to first vertex

                        }
                        else if ((Math.Abs(first_x - neighborvertex_3.x) < EPS) &&
                                   (Math.Abs(first_y - neighborvertex_3.y) < EPS))
                        {
                            firstVertexMatched = 13; // neighbor's 3rd vertex is matched to first vertex

                        }/*else{	
                     // none of them matched
                } // end of first vertex matching */

                        secondVertexMatched = 0;
                        if ((Math.Abs(second_x - neighborvertex_1.x) < EPS) &&
                            (Math.Abs(second_y - neighborvertex_1.y) < EPS))
                        {
                            secondVertexMatched = 21; // neighbor's 1st vertex is matched to second vertex
                        }
                        else if ((Math.Abs(second_x - neighborvertex_2.x) < EPS) &&
                           (Math.Abs(second_y - neighborvertex_2.y) < EPS))
                        {
                            secondVertexMatched = 22; // neighbor's 2nd vertex is matched to second vertex
                        }
                        else if ((Math.Abs(second_x - neighborvertex_3.x) < EPS) &&
                               (Math.Abs(second_y - neighborvertex_3.y) < EPS))
                        {
                            secondVertexMatched = 23; // neighbor's 3rd vertex is matched to second vertex
                        }/*else{	
                    // none of them matched
                } // end of second vertex matching*/

                    }

                }// if neighbor exists or not

                if (((firstVertexMatched == 11) && (secondVertexMatched == 22 || secondVertexMatched == 23))
                 || ((firstVertexMatched == 12) && (secondVertexMatched == 21 || secondVertexMatched == 23))
                 || ((firstVertexMatched == 13) && (secondVertexMatched == 21 || secondVertexMatched == 22)))
                    break;
            }// end of for loop over all orientations

            switch (firstVertexMatched)
            {
                case 0:
                    notFound = true;
                    break;
                case 11:
                    if (secondVertexMatched == 22)
                    {
                        thirdpoint[0] = neighborvertex_3.x;
                        thirdpoint[1] = neighborvertex_3.y;
                    }
                    else if (secondVertexMatched == 23)
                    {
                        thirdpoint[0] = neighborvertex_2.x;
                        thirdpoint[1] = neighborvertex_2.y;
                    }
                    else { notFound = true; }
                    break;
                case 12:
                    if (secondVertexMatched == 21)
                    {
                        thirdpoint[0] = neighborvertex_3.x;
                        thirdpoint[1] = neighborvertex_3.y;
                    }
                    else if (secondVertexMatched == 23)
                    {
                        thirdpoint[0] = neighborvertex_1.x;
                        thirdpoint[1] = neighborvertex_1.y;
                    }
                    else { notFound = true; }
                    break;
                case 13:
                    if (secondVertexMatched == 21)
                    {
                        thirdpoint[0] = neighborvertex_2.x;
                        thirdpoint[1] = neighborvertex_2.y;
                    }
                    else if (secondVertexMatched == 22)
                    {
                        thirdpoint[0] = neighborvertex_1.x;
                        thirdpoint[1] = neighborvertex_1.y;
                    }
                    else { notFound = true; }
                    break;
                default:
                    if (secondVertexMatched == 0) { notFound = true; }
                    break;
            }
            // pointer of the neighbor triangle
            neighotri = neighbor;
            return notFound;

        }

        /// <summary>
        /// Find a new point location by wedge intersection.
        /// </summary>
        /// <param name="numpoints"></param>
        /// <param name="points"></param>
        /// <param name="newloc">A new location for the point according to surrounding points.</param>
        /// <returns>Returns true if new location found</returns>
        private bool GetWedgeIntersectionWithoutMaxAngle(int numpoints,
            double[] points, ref double[] newloc)
        {
            //double total_x = 0;
            //double total_y = 0;
            double x0, y0, x1, y1, x2, y2;
            //double compConst = 0.01; // for comparing real numbers

            double x01, y01;
            //double x12, y12;

            //double ax, ay, bx, by; //two intersections of two petals disks

            double d01;//, d12

            //double petalx0, petaly0, petalr0, petalx1, petaly1, petalr1; 

            //double p[5];

            // Resize work arrays
            if (2 * numpoints > petalx.Length)
            {
                petalx = new double[2 * numpoints];
                petaly = new double[2 * numpoints];
                petalr = new double[2 * numpoints];
                wedges = new double[2 * numpoints * 16 + 36];
            }

            double xmid, ymid, dist, x3, y3;
            double x_1, y_1, x_2, y_2, x_3, y_3, x_4, y_4, tempx, tempy;
            double ux, uy;
            double alpha;
            double[] p1 = new double[3];

            //double poly_points;
            int numpolypoints = 0;

            //int numBadTriangle;

            int i, j;

            int s, flag, count, num;

            double petalcenterconstant, petalradiusconstant;

            x0 = points[2 * numpoints - 4];
            y0 = points[2 * numpoints - 3];
            x1 = points[2 * numpoints - 2];
            y1 = points[2 * numpoints - 1];

            // minimum angle
            alpha = behavior.MinAngle * Math.PI / 180.0;
            // initialize the constants
            if (behavior.goodAngle == 1.0)
            {
                petalcenterconstant = 0;
                petalradiusconstant = 0;
            }
            else
            {
                petalcenterconstant = 0.5 / Math.Tan(alpha);
                petalradiusconstant = 0.5 / Math.Sin(alpha);
            }

            for (i = 0; i < numpoints * 2; i = i + 2)
            {
                x2 = points[i];
                y2 = points[i + 1];

                //printf("POLYGON POINTS (p,q) #%d (%.12f, %.12f) (%.12f, %.12f)\n", i/2, x0, y0,x1, y1);

                x01 = x1 - x0;
                y01 = y1 - y0;
                d01 = Math.Sqrt(x01 * x01 + y01 * y01);
                // find the petal of each edge 01;

                //	    printf("PETAL CONSTANT (%.12f, %.12f)\n", 
                //	   b.petalcenterconstant,  b.petalradiusconstant );
                //	    printf("PETAL DIFFS (%.6f, %.6f, %.4f)\n", x01, y01, d01);

                petalx[i / 2] = x0 + 0.5 * x01 - petalcenterconstant * y01;
                petaly[i / 2] = y0 + 0.5 * y01 + petalcenterconstant * x01;
                petalr[i / 2] = petalradiusconstant * d01;
                petalx[numpoints + i / 2] = petalx[i / 2];
                petaly[numpoints + i / 2] = petaly[i / 2];
                petalr[numpoints + i / 2] = petalr[i / 2];
                //printf("PETAL POINTS #%d (%.12f, %.12f) R= %.12f\n", i/2, petalx[i/2],petaly[i/2], petalr[i/2]);

                /// FIRST FIND THE HALF-PLANE POINTS FOR EACH PETAL
                xmid = (x0 + x1) / 2.0;	// mid point of pq
                ymid = (y0 + y1) / 2.0;

                // distance between xmid and petal center	
                dist = Math.Sqrt((petalx[i / 2] - xmid) * (petalx[i / 2] - xmid) + (petaly[i / 2] - ymid) * (petaly[i / 2] - ymid));
                // find the unit vector goes from mid point to petal center
                ux = (petalx[i / 2] - xmid) / dist;
                uy = (petaly[i / 2] - ymid) / dist;
                // find the third point other than p and q
                x3 = petalx[i / 2] + ux * petalr[i / 2];
                y3 = petaly[i / 2] + uy * petalr[i / 2];
                /// FIND THE LINE POINTS BY THE ROTATION MATRIX
                // cw rotation matrix [cosX sinX; -sinX cosX]
                // cw rotation about (x,y) [ux*cosX + uy*sinX + x - x*cosX - y*sinX; -ux*sinX + uy*cosX + y + x*sinX - y*cosX]
                // ccw rotation matrix [cosX -sinX; sinX cosX]
                // ccw rotation about (x,y) [ux*cosX - uy*sinX + x - x*cosX + y*sinX; ux*sinX + uy*cosX + y - x*sinX - y*cosX]
                /// LINE #1: (x1,y1) & (x_1,y_1) 
                // vector from p to q
                ux = x1 - x0;
                uy = y1 - y0;
                // rotate the vector around p = (x0,y0) in ccw by alpha degrees
                x_1 = x1 * Math.Cos(alpha) - y1 * Math.Sin(alpha) + x0 - x0 * Math.Cos(alpha) + y0 * Math.Sin(alpha);
                y_1 = x1 * Math.Sin(alpha) + y1 * Math.Cos(alpha) + y0 - x0 * Math.Sin(alpha) - y0 * Math.Cos(alpha);
                // add these to wedges list as lines in order	
                wedges[i * 16] = x0; wedges[i * 16 + 1] = y0;
                wedges[i * 16 + 2] = x_1; wedges[i * 16 + 3] = y_1;
                //printf("LINE #1 (%.12f, %.12f) (%.12f, %.12f)\n", x0,y0,x_1,y_1);
                /// LINE #2: (x2,y2) & (x_2,y_2) 
                // vector from p to q
                ux = x0 - x1;
                uy = y0 - y1;
                // rotate the vector around q = (x1,y1) in cw by alpha degrees
                x_2 = x0 * Math.Cos(alpha) + y0 * Math.Sin(alpha) + x1 - x1 * Math.Cos(alpha) - y1 * Math.Sin(alpha);
                y_2 = -x0 * Math.Sin(alpha) + y0 * Math.Cos(alpha) + y1 + x1 * Math.Sin(alpha) - y1 * Math.Cos(alpha);
                // add these to wedges list as lines in order	
                wedges[i * 16 + 4] = x_2; wedges[i * 16 + 5] = y_2;
                wedges[i * 16 + 6] = x1; wedges[i * 16 + 7] = y1;
                //printf("LINE #2 (%.12f, %.12f) (%.12f, %.12f)\n", x_2,y_2,x1,y1);
                // vector from (petalx, petaly) to (x3,y3)
                ux = x3 - petalx[i / 2];
                uy = y3 - petaly[i / 2];
                tempx = x3; tempy = y3;
                /// LINE #3, #4, #5: (x3,y3) & (x_3,y_3) 
                for (j = 1; j < 4; j++)
                {
                    // rotate the vector around (petalx,petaly) in cw by (60 - alpha)*j degrees			
                    x_3 = x3 * Math.Cos((Math.PI / 3.0 - alpha) * j) + y3 * Math.Sin((Math.PI / 3.0 - alpha) * j) + petalx[i / 2] - petalx[i / 2] * Math.Cos((Math.PI / 3.0 - alpha) * j) - petaly[i / 2] * Math.Sin((Math.PI / 3.0 - alpha) * j);
                    y_3 = -x3 * Math.Sin((Math.PI / 3.0 - alpha) * j) + y3 * Math.Cos((Math.PI / 3.0 - alpha) * j) + petaly[i / 2] + petalx[i / 2] * Math.Sin((Math.PI / 3.0 - alpha) * j) - petaly[i / 2] * Math.Cos((Math.PI / 3.0 - alpha) * j);
                    // add these to wedges list as lines in order	
                    wedges[i * 16 + 8 + 4 * (j - 1)] = x_3; wedges[i * 16 + 9 + 4 * (j - 1)] = y_3;
                    wedges[i * 16 + 10 + 4 * (j - 1)] = tempx; wedges[i * 16 + 11 + 4 * (j - 1)] = tempy;
                    tempx = x_3; tempy = y_3;
                }
                tempx = x3; tempy = y3;
                /// LINE #6, #7, #8: (x3,y3) & (x_4,y_4) 
                for (j = 1; j < 4; j++)
                {
                    // rotate the vector around (petalx,petaly) in ccw by (60 - alpha)*j degrees
                    x_4 = x3 * Math.Cos((Math.PI / 3.0 - alpha) * j) - y3 * Math.Sin((Math.PI / 3.0 - alpha) * j) + petalx[i / 2] - petalx[i / 2] * Math.Cos((Math.PI / 3.0 - alpha) * j) + petaly[i / 2] * Math.Sin((Math.PI / 3.0 - alpha) * j);
                    y_4 = x3 * Math.Sin((Math.PI / 3.0 - alpha) * j) + y3 * Math.Cos((Math.PI / 3.0 - alpha) * j) + petaly[i / 2] - petalx[i / 2] * Math.Sin((Math.PI / 3.0 - alpha) * j) - petaly[i / 2] * Math.Cos((Math.PI / 3.0 - alpha) * j);

                    // add these to wedges list as lines in order	
                    wedges[i * 16 + 20 + 4 * (j - 1)] = tempx; wedges[i * 16 + 21 + 4 * (j - 1)] = tempy;
                    wedges[i * 16 + 22 + 4 * (j - 1)] = x_4; wedges[i * 16 + 23 + 4 * (j - 1)] = y_4;
                    tempx = x_4; tempy = y_4;
                }
                //printf("LINE #3 (%.12f, %.12f) (%.12f, %.12f)\n", x_3,y_3,x3,y3);			
                //printf("LINE #4 (%.12f, %.12f) (%.12f, %.12f)\n", x3,y3,x_4,y_4);

                /// IF IT IS THE FIRST ONE, FIND THE CONVEX POLYGON
                if (i == 0)
                {
                    // line1 & line2: p1
                    LineLineIntersection(x0, y0, x_1, y_1, x1, y1, x_2, y_2, ref p1);
                    if ((p1[0] == 1.0))
                    {
                        // #0
                        initialConvexPoly[0] = p1[1]; initialConvexPoly[1] = p1[2];
                        // #1
                        initialConvexPoly[2] = wedges[i * 16 + 16]; initialConvexPoly[3] = wedges[i * 16 + 17];
                        // #2
                        initialConvexPoly[4] = wedges[i * 16 + 12]; initialConvexPoly[5] = wedges[i * 16 + 13];
                        // #3
                        initialConvexPoly[6] = wedges[i * 16 + 8]; initialConvexPoly[7] = wedges[i * 16 + 9];
                        // #4
                        initialConvexPoly[8] = x3; initialConvexPoly[9] = y3;
                        // #5
                        initialConvexPoly[10] = wedges[i * 16 + 22]; initialConvexPoly[11] = wedges[i * 16 + 23];
                        // #6
                        initialConvexPoly[12] = wedges[i * 16 + 26]; initialConvexPoly[13] = wedges[i * 16 + 27];
                        // #7
                        initialConvexPoly[14] = wedges[i * 16 + 30]; initialConvexPoly[15] = wedges[i * 16 + 31];
                        //printf("INITIAL POLY [%.12f, %.12f;%.12f, %.12f;%.12f, %.12f;%.12f, %.12f;%.12f, %.12f;%.12f, %.12f;%.12f, %.12f;%.12f, %.12f]\n", initialConvexPoly[0],initialConvexPoly[1],initialConvexPoly[2],initialConvexPoly[3],initialConvexPoly[4],initialConvexPoly[5],initialConvexPoly[6],initialConvexPoly[7],initialConvexPoly[8],initialConvexPoly[9],initialConvexPoly[10],initialConvexPoly[11],initialConvexPoly[12],initialConvexPoly[13],initialConvexPoly[14],initialConvexPoly[15]);
                    }
                }

                x0 = x1; y0 = y1;
                x1 = x2; y1 = y2;
            }

            /// HALF PLANE INTERSECTION: START SPLITTING THE INITIAL POLYGON TO FIND FEASIBLE REGION    
            if (numpoints != 0)
            {
                // first intersect the opposite located ones
                s = (numpoints - 1) / 2 + 1;
                flag = 0;
                count = 0;
                i = 1;
                num = 8;
                for (j = 0; j < 32; j = j + 4)
                {
                    numpolypoints = HalfPlaneIntersection(num, ref initialConvexPoly, wedges[32 * s + j], wedges[32 * s + 1 + j], wedges[32 * s + 2 + j], wedges[32 * s + 3 + j]);
                    if (numpolypoints == 0)
                        return false;
                    else
                        num = numpolypoints;
                }
                count++;
                while (count < numpoints - 1)
                {
                    for (j = 0; j < 32; j = j + 4)
                    {
                        numpolypoints = HalfPlaneIntersection(num, ref initialConvexPoly, wedges[32 * (i + s * flag) + j], wedges[32 * (i + s * flag) + 1 + j], wedges[32 * (i + s * flag) + 2 + j], wedges[32 * (i + s * flag) + 3 + j]);
                        if (numpolypoints == 0)
                            return false;
                        else
                            num = numpolypoints;
                    }
                    i = i + flag;
                    flag = (flag + 1) % 2;
                    count++;
                }
                /// IF THERE IS A FEASIBLE INTERSECTION POLYGON, FIND ITS CENTROID AS THE NEW LOCATION
                FindPolyCentroid(numpolypoints, initialConvexPoly, ref newloc);

                if (behavior.fixedArea)
                {
                    // 		numBadTriangle = 0;
                    // 		for(j= 0; j < numpoints *2-2; j = j+2){
                    // 			if(testTriangleAngleArea(m,b,&newloc[0],&newloc[1], &points[j], &points[j+1], &points[j+2], &points[j+3] )){
                    // 				numBadTriangle++; 
                    // 			}
                    // 		}
                    // 		if(testTriangleAngleArea(m,b, &newloc[0],&newloc[1], &points[0], &points[1], &points[numpoints*2-2], &points[numpoints*2-1] )){
                    // 			numBadTriangle++;
                    // 		}
                    // 		
                    // 		if (numBadTriangle == 0)  {
                    // 			
                    // 			return 1;
                    // 		}
                }
                else
                {
                    //printf("yes, we found a feasible region num: %d newloc (%.12f,%.12f)\n", numpolypoints, newloc[0], newloc[1]);
                    // 	for(i = 0; i < 2*numpolypoints; i = i+2){
                    // 		printf("point %d) (%.12f,%.12f)\n", i/2, initialConvexPoly[i], initialConvexPoly[i+1]);
                    // 	}	
                    // 	printf("numpoints %d\n",numpoints);
                    return true;
                }
            }


            return false;
        }

        /// <summary>
        /// Find a new point location by wedge intersection.
        /// </summary>
        /// <param name="numpoints"></param>
        /// <param name="points"></param>
        /// <param name="newloc">A new location for the point according to surrounding points.</param>
        /// <returns>Returns true if new location found</returns>
        private bool GetWedgeIntersection(int numpoints, double[] points, ref double[] newloc)
        {
            //double total_x = 0;
            //double total_y = 0;
            double x0, y0, x1, y1, x2, y2;
            //double compConst = 0.01; // for comparing real numbers

            double x01, y01;
            //double x12, y12;

            //double ax, ay, bx, by;  //two intersections of two petals disks

            double d01;//, d12

            //double petalx0, petaly1, petaly0, petalr0, petalx1, petalr1; 

            //double p[5];

            // Resize work arrays
            if (2 * numpoints > petalx.Length)
            {
                petalx = new double[2 * numpoints];
                petaly = new double[2 * numpoints];
                petalr = new double[2 * numpoints];
                wedges = new double[2 * numpoints * 20 + 40];
            }

            double xmid, ymid, dist, x3, y3;
            double x_1, y_1, x_2, y_2, x_3, y_3, x_4, y_4, tempx, tempy, x_5, y_5, x_6, y_6;
            double ux, uy;

            double[] p1 = new double[3];
            double[] p2 = new double[3];
            double[] p3 = new double[3];
            double[] p4 = new double[3];

            //double poly_points;
            int numpolypoints = 0;
            int howManyPoints = 0;	// keeps the number of points used for representing the wedge
            double line345 = 4.0, line789 = 4.0; // flag keeping which line to skip or construct

            int numBadTriangle;

            int i, j, k;

            int s, flag, count, num;

            int n, e;

            double weight;

            double petalcenterconstant, petalradiusconstant;

            x0 = points[2 * numpoints - 4];
            y0 = points[2 * numpoints - 3];
            x1 = points[2 * numpoints - 2];
            y1 = points[2 * numpoints - 1];

            // minimum / maximum angle
            double alpha, sinAlpha, cosAlpha, beta, sinBeta, cosBeta;
            alpha = behavior.MinAngle * Math.PI / 180.0;
            sinAlpha = Math.Sin(alpha);
            cosAlpha = Math.Cos(alpha);
            beta = behavior.MaxAngle * Math.PI / 180.0;
            sinBeta = Math.Sin(beta);
            cosBeta = Math.Cos(beta);

            // initialize the constants
            if (behavior.goodAngle == 1.0)
            {
                petalcenterconstant = 0;
                petalradiusconstant = 0;
            }
            else
            {
                petalcenterconstant = 0.5 / Math.Tan(alpha);
                petalradiusconstant = 0.5 / Math.Sin(alpha);
            }

            for (i = 0; i < numpoints * 2; i = i + 2)
            {
                // go to the next point
                x2 = points[i];
                y2 = points[i + 1];

                //   	printf("POLYGON POINTS (p,q) #%d (%.12f, %.12f) (%.12f, %.12f)\n", i/2, x0, y0,x1, y1);

                x01 = x1 - x0;
                y01 = y1 - y0;
                d01 = Math.Sqrt(x01 * x01 + y01 * y01);
                // find the petal of each edge 01;

                //	    printf("PETAL CONSTANT (%.12f, %.12f)\n", 
                //	   b.petalcenterconstant,  b.petalradiusconstant );
                //	    printf("PETAL DIFFS (%.6f, %.6f, %.4f)\n", x01, y01, d01);
                //printf("i:%d numpoints:%d\n", i, numpoints);
                petalx[i / 2] = x0 + 0.5 * x01 - petalcenterconstant * y01;
                petaly[i / 2] = y0 + 0.5 * y01 + petalcenterconstant * x01;
                petalr[i / 2] = petalradiusconstant * d01;
                petalx[numpoints + i / 2] = petalx[i / 2];
                petaly[numpoints + i / 2] = petaly[i / 2];
                petalr[numpoints + i / 2] = petalr[i / 2];
                //printf("PETAL POINTS #%d (%.12f, %.12f) R= %.12f\n", i/2, petalx[i/2],petaly[i/2], petalr[i/2]);

                /// FIRST FIND THE HALF-PLANE POINTS FOR EACH PETAL
                xmid = (x0 + x1) / 2.0;	// mid point of pq
                ymid = (y0 + y1) / 2.0;

                // distance between xmid and petal center	
                dist = Math.Sqrt((petalx[i / 2] - xmid) * (petalx[i / 2] - xmid) + (petaly[i / 2] - ymid) * (petaly[i / 2] - ymid));
                // find the unit vector goes from mid point to petal center
                ux = (petalx[i / 2] - xmid) / dist;
                uy = (petaly[i / 2] - ymid) / dist;
                // find the third point other than p and q
                x3 = petalx[i / 2] + ux * petalr[i / 2];
                y3 = petaly[i / 2] + uy * petalr[i / 2];
                /// FIND THE LINE POINTS BY THE ROTATION MATRIX
                // cw rotation matrix [cosX sinX; -sinX cosX]
                // cw rotation about (x,y) [ux*cosX + uy*sinX + x - x*cosX - y*sinX; -ux*sinX + uy*cosX + y + x*sinX - y*cosX]
                // ccw rotation matrix [cosX -sinX; sinX cosX]
                // ccw rotation about (x,y) [ux*cosX - uy*sinX + x - x*cosX + y*sinX; ux*sinX + uy*cosX + y - x*sinX - y*cosX]
                /// LINE #1: (x1,y1) & (x_1,y_1) 
                // vector from p to q
                ux = x1 - x0;
                uy = y1 - y0;
                // rotate the vector around p = (x0,y0) in ccw by alpha degrees
                x_1 = x1 * cosAlpha - y1 * sinAlpha + x0 - x0 * cosAlpha + y0 * sinAlpha;
                y_1 = x1 * sinAlpha + y1 * cosAlpha + y0 - x0 * sinAlpha - y0 * cosAlpha;
                // add these to wedges list as lines in order	
                wedges[i * 20] = x0; wedges[i * 20 + 1] = y0;
                wedges[i * 20 + 2] = x_1; wedges[i * 20 + 3] = y_1;
                //printf("LINE #1 (%.12f, %.12f) (%.12f, %.12f)\n", x0,y0,x_1,y_1);
                /// LINE #2: (x2,y2) & (x_2,y_2) 
                // vector from q to p
                ux = x0 - x1;
                uy = y0 - y1;
                // rotate the vector around q = (x1,y1) in cw by alpha degrees
                x_2 = x0 * cosAlpha + y0 * sinAlpha + x1 - x1 * cosAlpha - y1 * sinAlpha;
                y_2 = -x0 * sinAlpha + y0 * cosAlpha + y1 + x1 * sinAlpha - y1 * cosAlpha;
                // add these to wedges list as lines in order	
                wedges[i * 20 + 4] = x_2; wedges[i * 20 + 5] = y_2;
                wedges[i * 20 + 6] = x1; wedges[i * 20 + 7] = y1;
                //printf("LINE #2 (%.12f, %.12f) (%.12f, %.12f)\n", x_2,y_2,x1,y1);
                // vector from (petalx, petaly) to (x3,y3)
                ux = x3 - petalx[i / 2];
                uy = y3 - petaly[i / 2];
                tempx = x3; tempy = y3;

                /// DETERMINE HOW MANY POINTS TO USE ACCORDING TO THE MINANGLE-MAXANGLE COMBINATION
                // petal center angle
                alpha = (2.0 * behavior.MaxAngle + behavior.MinAngle - 180.0);
                if (alpha <= 0.0)
                {// when only angle lines needed
                    // 4 point case
                    howManyPoints = 4;
                    //printf("4 point case\n");
                    line345 = 1.0;
                    line789 = 1.0;
                }
                else if (alpha <= 5.0)
                {// when only angle lines plus two other lines are needed
                    // 6 point case
                    howManyPoints = 6;
                    //printf("6 point case\n");
                    line345 = 2.0;
                    line789 = 2.0;
                }
                else if (alpha <= 10.0)
                {// when we need more lines
                    // 8 point case
                    howManyPoints = 8;
                    line345 = 3.0;
                    line789 = 3.0;
                    //printf("8 point case\n");
                }
                else
                {// when we have a big wedge
                    // 10 point case
                    howManyPoints = 10;
                    //printf("10 point case\n");
                    line345 = 4.0;
                    line789 = 4.0;
                }
                alpha = alpha * Math.PI / 180.0;
                /// LINE #3, #4, #5: (x3,y3) & (x_3,y_3) 
                for (j = 1; j < line345; j++)
                {
                    if (line345 == 1)
                        continue;
                    // rotate the vector around (petalx,petaly) in cw by (alpha/3.0)*j degrees			
                    x_3 = x3 * Math.Cos((alpha / (line345 - 1.0)) * j) + y3 * Math.Sin(((alpha / (line345 - 1.0)) * j)) + petalx[i / 2] - petalx[i / 2] * Math.Cos(((alpha / (line345 - 1.0)) * j)) - petaly[i / 2] * Math.Sin(((alpha / (line345 - 1.0)) * j));
                    y_3 = -x3 * Math.Sin(((alpha / (line345 - 1.0)) * j)) + y3 * Math.Cos(((alpha / (line345 - 1.0)) * j)) + petaly[i / 2] + petalx[i / 2] * Math.Sin(((alpha / (line345 - 1.0)) * j)) - petaly[i / 2] * Math.Cos(((alpha / (line345 - 1.0)) * j));
                    // add these to wedges list as lines in order	
                    wedges[i * 20 + 8 + 4 * (j - 1)] = x_3; wedges[i * 20 + 9 + 4 * (j - 1)] = y_3;
                    wedges[i * 20 + 10 + 4 * (j - 1)] = tempx; wedges[i * 20 + 11 + 4 * (j - 1)] = tempy;
                    tempx = x_3; tempy = y_3;
                }
                /// LINE #6: (x2,y2) & (x_3,y_3) 
                // vector from q to p
                ux = x0 - x1;
                uy = y0 - y1;
                // rotate the vector around q = (x1,y1) in cw by alpha degrees
                x_5 = x0 * cosBeta + y0 * sinBeta + x1 - x1 * cosBeta - y1 * sinBeta;
                y_5 = -x0 * sinBeta + y0 * cosBeta + y1 + x1 * sinBeta - y1 * cosBeta;
                wedges[i * 20 + 20] = x1; wedges[i * 20 + 21] = y1;
                wedges[i * 20 + 22] = x_5; wedges[i * 20 + 23] = y_5;

                tempx = x3; tempy = y3;
                /// LINE #7, #8, #9: (x3,y3) & (x_4,y_4) 
                for (j = 1; j < line789; j++)
                {
                    if (line789 == 1)
                        continue;
                    // rotate the vector around (petalx,petaly) in ccw by (alpha/3.0)*j degrees
                    x_4 = x3 * Math.Cos((alpha / (line789 - 1.0)) * j) - y3 * Math.Sin((alpha / (line789 - 1.0)) * j) + petalx[i / 2] - petalx[i / 2] * Math.Cos((alpha / (line789 - 1.0)) * j) + petaly[i / 2] * Math.Sin((alpha / (line789 - 1.0)) * j);
                    y_4 = x3 * Math.Sin((alpha / (line789 - 1.0)) * j) + y3 * Math.Cos((alpha / (line789 - 1.0)) * j) + petaly[i / 2] - petalx[i / 2] * Math.Sin((alpha / (line789 - 1.0)) * j) - petaly[i / 2] * Math.Cos((alpha / (line789 - 1.0)) * j);

                    // add these to wedges list as lines in order	
                    wedges[i * 20 + 24 + 4 * (j - 1)] = tempx; wedges[i * 20 + 25 + 4 * (j - 1)] = tempy;
                    wedges[i * 20 + 26 + 4 * (j - 1)] = x_4; wedges[i * 20 + 27 + 4 * (j - 1)] = y_4;
                    tempx = x_4; tempy = y_4;
                }
                /// LINE #10: (x1,y1) & (x_3,y_3) 
                // vector from p to q
                ux = x1 - x0;
                uy = y1 - y0;
                // rotate the vector around p = (x0,y0) in ccw by alpha degrees
                x_6 = x1 * cosBeta - y1 * sinBeta + x0 - x0 * cosBeta + y0 * sinBeta;
                y_6 = x1 * sinBeta + y1 * cosBeta + y0 - x0 * sinBeta - y0 * cosBeta;
                wedges[i * 20 + 36] = x_6; wedges[i * 20 + 37] = y_6;
                wedges[i * 20 + 38] = x0; wedges[i * 20 + 39] = y0;

                //printf("LINE #1 (%.12f, %.12f) (%.12f, %.12f)\n", x0,y0,x_1,y_1);
                /// IF IT IS THE FIRST ONE, FIND THE CONVEX POLYGON
                if (i == 0)
                {
                    switch (howManyPoints)
                    {
                        case 4:
                            // line1 & line2 & line3 & line4
                            LineLineIntersection(x0, y0, x_1, y_1, x1, y1, x_2, y_2, ref p1);
                            LineLineIntersection(x0, y0, x_1, y_1, x1, y1, x_5, y_5, ref p2);
                            LineLineIntersection(x0, y0, x_6, y_6, x1, y1, x_5, y_5, ref p3);
                            LineLineIntersection(x0, y0, x_6, y_6, x1, y1, x_2, y_2, ref p4);
                            if ((p1[0] == 1.0) && (p2[0] == 1.0) && (p3[0] == 1.0) && (p4[0] == 1.0))
                            {
                                // #0
                                initialConvexPoly[0] = p1[1]; initialConvexPoly[1] = p1[2];
                                // #1
                                initialConvexPoly[2] = p2[1]; initialConvexPoly[3] = p2[2];
                                // #2
                                initialConvexPoly[4] = p3[1]; initialConvexPoly[5] = p3[2];
                                // #3
                                initialConvexPoly[6] = p4[1]; initialConvexPoly[7] = p4[2];
                            }
                            break;
                        case 6:
                            // line1 & line2 & line3
                            LineLineIntersection(x0, y0, x_1, y_1, x1, y1, x_2, y_2, ref p1);
                            LineLineIntersection(x0, y0, x_1, y_1, x1, y1, x_5, y_5, ref p2);
                            LineLineIntersection(x0, y0, x_6, y_6, x1, y1, x_2, y_2, ref p3);
                            if ((p1[0] == 1.0) && (p2[0] == 1.0) && (p3[0] == 1.0))
                            {
                                // #0
                                initialConvexPoly[0] = p1[1]; initialConvexPoly[1] = p1[2];
                                // #1
                                initialConvexPoly[2] = p2[1]; initialConvexPoly[3] = p2[2];
                                // #2
                                initialConvexPoly[4] = wedges[i * 20 + 8]; initialConvexPoly[5] = wedges[i * 20 + 9];
                                // #3
                                initialConvexPoly[6] = x3; initialConvexPoly[7] = y3;
                                // #4
                                initialConvexPoly[8] = wedges[i * 20 + 26]; initialConvexPoly[9] = wedges[i * 20 + 27];
                                // #5
                                initialConvexPoly[10] = p3[1]; initialConvexPoly[11] = p3[2];
                            }
                            break;
                        case 8:
                            // line1 & line2: p1
                            LineLineIntersection(x0, y0, x_1, y_1, x1, y1, x_2, y_2, ref p1);
                            LineLineIntersection(x0, y0, x_1, y_1, x1, y1, x_5, y_5, ref p2);
                            LineLineIntersection(x0, y0, x_6, y_6, x1, y1, x_2, y_2, ref p3);
                            if ((p1[0] == 1.0) && (p2[0] == 1.0) && (p3[0] == 1.0))
                            {
                                // #0
                                initialConvexPoly[0] = p1[1]; initialConvexPoly[1] = p1[2];
                                // #1
                                initialConvexPoly[2] = p2[1]; initialConvexPoly[3] = p2[2];
                                // #2
                                initialConvexPoly[4] = wedges[i * 20 + 12]; initialConvexPoly[5] = wedges[i * 20 + 13];
                                // #3
                                initialConvexPoly[6] = wedges[i * 20 + 8]; initialConvexPoly[7] = wedges[i * 20 + 9];
                                // #4
                                initialConvexPoly[8] = x3; initialConvexPoly[9] = y3;
                                // #5
                                initialConvexPoly[10] = wedges[i * 20 + 26]; initialConvexPoly[11] = wedges[i * 20 + 27];
                                // #6
                                initialConvexPoly[12] = wedges[i * 20 + 30]; initialConvexPoly[13] = wedges[i * 20 + 31];
                                // #7
                                initialConvexPoly[14] = p3[1]; initialConvexPoly[15] = p3[2];
                            }
                            break;
                        case 10:
                            // line1 & line2: p1
                            LineLineIntersection(x0, y0, x_1, y_1, x1, y1, x_2, y_2, ref p1);
                            LineLineIntersection(x0, y0, x_1, y_1, x1, y1, x_5, y_5, ref p2);
                            LineLineIntersection(x0, y0, x_6, y_6, x1, y1, x_2, y_2, ref p3);
                            //printf("p3 %f %f %f (%f %f) (%f %f) (%f %f) (%f %f)\n",p3[0],p3[1],p3[2], x0, y0, x_6, x_6, x1, y1, x_2, y_2);
                            if ((p1[0] == 1.0) && (p2[0] == 1.0) && (p3[0] == 1.0))
                            {
                                // #0
                                initialConvexPoly[0] = p1[1]; initialConvexPoly[1] = p1[2];
                                // #1
                                initialConvexPoly[2] = p2[1]; initialConvexPoly[3] = p2[2];
                                // #2
                                initialConvexPoly[4] = wedges[i * 20 + 16]; initialConvexPoly[5] = wedges[i * 20 + 17];
                                // #3
                                initialConvexPoly[6] = wedges[i * 20 + 12]; initialConvexPoly[7] = wedges[i * 20 + 13];
                                // #4
                                initialConvexPoly[8] = wedges[i * 20 + 8]; initialConvexPoly[9] = wedges[i * 20 + 9];
                                // #5
                                initialConvexPoly[10] = x3; initialConvexPoly[11] = y3;
                                // #6
                                initialConvexPoly[12] = wedges[i * 20 + 28]; initialConvexPoly[13] = wedges[i * 20 + 29];
                                // #7
                                initialConvexPoly[14] = wedges[i * 20 + 32]; initialConvexPoly[15] = wedges[i * 20 + 33];
                                // #8
                                initialConvexPoly[16] = wedges[i * 20 + 34]; initialConvexPoly[17] = wedges[i * 20 + 35];
                                // #9
                                initialConvexPoly[18] = p3[1]; initialConvexPoly[19] = p3[2];
                            }
                            break;
                    }
                    // 		printf("smallest edge (%f,%f) (%f,%f)\n", x0,y0, x1,y1);
                    // 			printf("real INITIAL POLY [%.12f, %.12f;%.12f, %.12f;%.12f, %.12f;%.12f, %.12f;%.12f, %.12f;%.12f, %.12f;%.12f, %.12f;%.12f, %.12f;%.12f, %.12f;%.12f, %.12f;]\n", initialConvexPoly[0],initialConvexPoly[1],initialConvexPoly[2],initialConvexPoly[3],initialConvexPoly[4],initialConvexPoly[5],initialConvexPoly[6],initialConvexPoly[7],initialConvexPoly[8],initialConvexPoly[9],initialConvexPoly[10],initialConvexPoly[11],initialConvexPoly[12],initialConvexPoly[13],initialConvexPoly[14],initialConvexPoly[15],initialConvexPoly[16],initialConvexPoly[17],initialConvexPoly[18],initialConvexPoly[19]);
                }

                x0 = x1; y0 = y1;
                x1 = x2; y1 = y2;
            }
            /// HALF PLANE INTERSECTION: START SPLITTING THE INITIAL POLYGON TO FIND FEASIBLE REGION    
            if (numpoints != 0)
            {
                // first intersect the opposite located ones
                s = (numpoints - 1) / 2 + 1;
                flag = 0;
                count = 0;
                i = 1;
                num = howManyPoints;
                for (j = 0; j < 40; j = j + 4)
                {
                    // in order to skip non-existent lines
                    if (howManyPoints == 4 && (j == 8 || j == 12 || j == 16 || j == 24 || j == 28 || j == 32))
                    {
                        continue;
                    }
                    else if (howManyPoints == 6 && (j == 12 || j == 16 || j == 28 || j == 32))
                    {
                        continue;
                    }
                    else if (howManyPoints == 8 && (j == 16 || j == 32))
                    {
                        continue;
                    }
                    // 			printf("%d 1 INITIAL POLY [%.12f, %.12f;%.12f, %.12f;%.12f, %.12f;%.12f, %.12f;%.12f, %.12f;%.12f, %.12f;%.12f, %.12f;%.12f, %.12f;%.12f, %.12f;%.12f, %.12f;]\n",num, initialConvexPoly[0],initialConvexPoly[1],initialConvexPoly[2],initialConvexPoly[3],initialConvexPoly[4],initialConvexPoly[5],initialConvexPoly[6],initialConvexPoly[7],initialConvexPoly[8],initialConvexPoly[9],initialConvexPoly[10],initialConvexPoly[11],initialConvexPoly[12],initialConvexPoly[13],initialConvexPoly[14],initialConvexPoly[15],initialConvexPoly[16],initialConvexPoly[17],initialConvexPoly[18],initialConvexPoly[19]);	
                    // 			printf("line (%f, %f) (%f, %f)\n",wedges[40*s+j],wedges[40*s+1+j], wedges[40*s+2+j], wedges[40*s+3+j]);	
                    numpolypoints = HalfPlaneIntersection(num, ref initialConvexPoly, wedges[40 * s + j], wedges[40 * s + 1 + j], wedges[40 * s + 2 + j], wedges[40 * s + 3 + j]);

                    if (numpolypoints == 0)
                        return false;
                    else
                        num = numpolypoints;
                }
                count++;
                //printf("yes here\n");	
                while (count < numpoints - 1)
                {
                    for (j = 0; j < 40; j = j + 4)
                    {
                        // in order to skip non-existent lines
                        if (howManyPoints == 4 && (j == 8 || j == 12 || j == 16 || j == 24 || j == 28 || j == 32))
                        {
                            continue;
                        }
                        else if (howManyPoints == 6 && (j == 12 || j == 16 || j == 28 || j == 32))
                        {
                            continue;
                        }
                        else if (howManyPoints == 8 && (j == 16 || j == 32))
                        {
                            continue;
                        }
                        ////printf("%d 2 INITIAL POLY [%.12f, %.12f;%.12f, %.12f;%.12f, %.12f;%.12f, %.12f;%.12f, %.12f;%.12f, %.12f;%.12f, %.12f;%.12f, %.12f;%.12f, %.12f;%.12f, %.12f;]\n",numpolypoints, initialConvexPoly[0],initialConvexPoly[1],initialConvexPoly[2],initialConvexPoly[3],initialConvexPoly[4],initialConvexPoly[5],initialConvexPoly[6],initialConvexPoly[7],initialConvexPoly[8],initialConvexPoly[9],initialConvexPoly[10],initialConvexPoly[11],initialConvexPoly[12],initialConvexPoly[13],initialConvexPoly[14],initialConvexPoly[15],initialConvexPoly[16],initialConvexPoly[17],initialConvexPoly[18],initialConvexPoly[19]);	
                        //printf("line (%.20f, %.20f) (%.20f, %.20f)\n", wedges[40 * (i + s * flag) + j], wedges[40 * (i + s * flag) + 1 + j], wedges[40 * (i + s * flag) + 2 + j], wedges[40 * (i + s * flag) + 3 + j]);
                        numpolypoints = HalfPlaneIntersection(num, ref initialConvexPoly, wedges[40 * (i + s * flag) + j], wedges[40 * (i + s * flag) + 1 + j], wedges[40 * (i + s * flag) + 2 + j], wedges[40 * (i + s * flag) + 3 + j]);

                        if (numpolypoints == 0)
                            return false;
                        else
                            num = numpolypoints;
                    }
                    i = i + flag;
                    flag = (flag + 1) % 2;
                    count++;
                }
                /// IF THERE IS A FEASIBLE INTERSECTION POLYGON, FIND ITS CENTROID AS THE NEW LOCATION
                FindPolyCentroid(numpolypoints, initialConvexPoly, ref newloc);

                if (behavior.MaxAngle != 0.0)
                {
                    numBadTriangle = 0;
                    for (j = 0; j < numpoints * 2 - 2; j = j + 2)
                    {
                        if (IsBadTriangleAngle(newloc[0], newloc[1], points[j], points[j + 1], points[j + 2], points[j + 3]))
                        {
                            numBadTriangle++;
                        }
                    }
                    if (IsBadTriangleAngle(newloc[0], newloc[1], points[0], points[1], points[numpoints * 2 - 2], points[numpoints * 2 - 1]))
                    {
                        numBadTriangle++;
                    }

                    if (numBadTriangle == 0)
                    {

                        return true;
                    }
                    n = (numpoints <= 2) ? 20 : 30;
                    // try points other than centroid
                    for (k = 0; k < 2 * numpoints; k = k + 2)
                    {
                        for (e = 1; e < n; e = e + 1)
                        {
                            newloc[0] = 0.0; newloc[1] = 0.0;
                            for (i = 0; i < 2 * numpoints; i = i + 2)
                            {
                                weight = 1.0 / numpoints;
                                if (i == k)
                                {
                                    newloc[0] = newloc[0] + 0.1 * e * weight * points[i];
                                    newloc[1] = newloc[1] + 0.1 * e * weight * points[i + 1];
                                }
                                else
                                {
                                    weight = (1.0 - 0.1 * e * weight) / (double)(numpoints - 1.0);
                                    newloc[0] = newloc[0] + weight * points[i];
                                    newloc[1] = newloc[1] + weight * points[i + 1];
                                }

                            }
                            numBadTriangle = 0;
                            for (j = 0; j < numpoints * 2 - 2; j = j + 2)
                            {
                                if (IsBadTriangleAngle(newloc[0], newloc[1], points[j], points[j + 1], points[j + 2], points[j + 3]))
                                {
                                    numBadTriangle++;
                                }
                            }
                            if (IsBadTriangleAngle(newloc[0], newloc[1], points[0], points[1], points[numpoints * 2 - 2], points[numpoints * 2 - 1]))
                            {
                                numBadTriangle++;
                            }

                            if (numBadTriangle == 0)
                            {

                                return true;
                            }
                        }
                    }
                }
                else
                {
                    //printf("yes, we found a feasible region num: %d newloc (%.12f,%.12f)\n", numpolypoints, newloc[0], newloc[1]);
                    // 	for(i = 0; i < 2*numpolypoints; i = i+2){
                    // 		printf("point %d) (%.12f,%.12f)\n", i/2, initialConvexPoly[i], initialConvexPoly[i+1]);
                    // 	}	
                    // 	printf("numpoints %d\n",numpoints);
                    return true;
                }
            }


            return false;
        }

        /// <summary>
        /// Check polygon for min angle.
        /// </summary>
        /// <param name="numpoints"></param>
        /// <param name="points"></param>
        /// <returns>Returns true if the polygon has angles greater than 2*minangle.</returns>
        private bool ValidPolygonAngles(int numpoints, double[] points)
        {
            int i;//,j
            for (i = 0; i < numpoints; i++)
            {
                if (i == numpoints - 1)
                {
                    if (IsBadPolygonAngle(points[i * 2], points[i * 2 + 1], points[0], points[1], points[2], points[3]))
                    {
                        return false;	// one of the inner angles is less than required
                    }
                }
                else if (i == numpoints - 2)
                {
                    if (IsBadPolygonAngle(points[i * 2], points[i * 2 + 1], points[(i + 1) * 2], points[(i + 1) * 2 + 1], points[0], points[1]))
                    {
                        return false;	// one of the inner angles is less than required
                    }
                }
                else
                {
                    if (IsBadPolygonAngle(points[i * 2], points[i * 2 + 1], points[(i + 1) * 2], points[(i + 1) * 2 + 1], points[(i + 2) * 2], points[(i + 2) * 2 + 1]))
                    {
                        return false;	// one of the inner angles is less than required
                    }
                }
            }
            return true;	// all angles are valid
        }

        /// <summary>
        /// Given three coordinates of a polygon, tests to see if it satisfies the minimum 
        /// angle condition for relocation.
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="x3"></param>
        /// <param name="y3"></param>
        /// <returns>Returns true, if it is a BAD polygon corner, returns false if it is a GOOD 
        /// polygon corner</returns>
        private bool IsBadPolygonAngle(double x1, double y1,
                        double x2, double y2, double x3, double y3)
        {
            // variables keeping the distance values for the edges
            double dx12, dy12, dx23, dy23, dx31, dy31;
            double dist12, dist23, dist31;

            double cosAngle;    // in order to check minimum angle condition

            // calculate the side lengths

            dx12 = x1 - x2;
            dy12 = y1 - y2;
            dx23 = x2 - x3;
            dy23 = y2 - y3;
            dx31 = x3 - x1;
            dy31 = y3 - y1;
            // calculate the squares of the side lentghs
            dist12 = dx12 * dx12 + dy12 * dy12;
            dist23 = dx23 * dx23 + dy23 * dy23;
            dist31 = dx31 * dx31 + dy31 * dy31;

            /// calculate cosine of largest angle	///	
            cosAngle = (dist12 + dist23 - dist31) / (2 * Math.Sqrt(dist12) * Math.Sqrt(dist23));
            // Check whether the angle is smaller than permitted which is 2*minangle!!!  
            //printf("angle: %f 2*minangle = %f\n",acos(cosAngle)*180/PI, 2*acos(Math.Sqrt(b.goodangle))*180/PI);
            if (Math.Acos(cosAngle) < 2 * Math.Acos(Math.Sqrt(behavior.goodAngle)))
            {
                return true;// it is a BAD triangle
            }
            return false;// it is a GOOD triangle

        }

        /// <summary>
        /// Given four points representing two lines, returns the intersection point.
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="x3"></param>
        /// <param name="y3"></param>
        /// <param name="x4"></param>
        /// <param name="y4"></param>
        /// <param name="p">The intersection point.</param>
        /// <remarks>
        // referenced to: http://local.wasp.uwa.edu.au/~pbourke/geometry/
        /// </remarks>
        private void LineLineIntersection(
            double x1, double y1,
            double x2, double y2,
            double x3, double y3,
            double x4, double y4, ref double[] p)
        {
            // x1,y1  P1 coordinates (point of line 1)
            // x2,y2  P2 coordinates (point of line 1)	
            // x3,y3  P3 coordinates (point of line 2)
            // x4,y4  P4 coordinates (point of line 2)
            // p[1],p[2]   intersection coordinates
            //
            // This function returns a pointer array which first index indicates
            // weather they intersect on one point or not, followed by coordinate pairs.

            double u_a, u_b, denom;

            // calculate denominator first
            denom = (y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1);
            u_a = (x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3);
            u_b = (x2 - x1) * (y1 - y3) - (y2 - y1) * (x1 - x3);
            // if denominator and numerator equal to zero, lines are coincident
            if (Math.Abs(denom - 0.0) < EPS && (Math.Abs(u_b - 0.0) < EPS && Math.Abs(u_a - 0.0) < EPS))
            {
                p[0] = 0.0;
            }
            // if denominator equals to zero, lines are parallel
            else if (Math.Abs(denom - 0.0) < EPS)
            {
                p[0] = 0.0;
            }
            else
            {
                p[0] = 1.0;
                u_a = u_a / denom;
                u_b = u_b / denom;
                p[1] = x1 + u_a * (x2 - x1); // not the intersection point
                p[2] = y1 + u_a * (y2 - y1);
            }
        }

        /// <summary>
        /// Returns the convex polygon which is the intersection of the given convex 
        /// polygon with the halfplane on the left side (regarding the directional vector) 
        /// of the given line.
        /// </summary>
        /// <param name="numvertices"></param>
        /// <param name="convexPoly"></param>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <returns></returns>
        /// <remarks>
        /// http://www.mathematik.uni-ulm.de/stochastik/lehre/ws03_04/rt/Geometry2D.ps
        /// </remarks>
        private int HalfPlaneIntersection(int numvertices, ref double[] convexPoly, double x1, double y1, double x2, double y2)
        {
            double dx, dy;	// direction of the line
            double z, min, max;
            int i, j;

            int numpolys;
            double[] res = null;
            int count = 0;
            int intFound = 0;
            dx = x2 - x1;
            dy = y2 - y1;
            numpolys = SplitConvexPolygon(numvertices, convexPoly, x1, y1, x2, y2, polys);

            if (numpolys == 3)
            {
                count = numvertices;
            }
            else
            {
                for (i = 0; i < numpolys; i++)
                {
                    min = double.MaxValue;
                    max = double.MinValue;
                    // compute the minimum and maximum of the
                    // third coordinate of the cross product		
                    for (j = 1; j <= 2 * polys[i][0] - 1; j = j + 2)
                    {
                        z = dx * (polys[i][j + 1] - y1) - dy * (polys[i][j] - x1);
                        min = (z < min ? z : min);
                        max = (z > max ? z : max);
                    }
                    // ... and choose the (absolute) greater of both
                    z = (Math.Abs(min) > Math.Abs(max) ? min : max);
                    // and if it is positive, the polygon polys[i]
                    // is on the left side of line
                    if (z > 0.0)
                    {
                        res = polys[i];
                        intFound = 1;
                        break;
                    }
                }
                if (intFound == 1)
                {
                    while (count < res[0])
                    {
                        convexPoly[2 * count] = res[2 * count + 1];
                        convexPoly[2 * count + 1] = res[2 * count + 2];
                        count++;

                    }
                }
            }
            // update convexPoly
            return count;
        }

        /// <summary>
        /// Splits a convex polygons into one or two polygons through the intersection 
        /// with the given line (regarding the directional vector of the given line).
        /// </summary>
        /// <param name="numvertices"></param>
        /// <param name="convexPoly"></param>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="polys"></param>
        /// <returns></returns>
        /// <remarks>
        /// http://www.mathematik.uni-ulm.de/stochastik/lehre/ws03_04/rt/Geometry2D.ps
        /// </remarks>
        private int SplitConvexPolygon(int numvertices, double[] convexPoly, double x1, double y1, double x2, double y2, double[][] polys)
        {
            // state = 0: before the first intersection (with the line)
            // state = 1: after the first intersection (with the line)
            // state = 2: after the second intersection (with the line)

            int state = 0;
            double[] p = new double[3];
            int poly1counter = 0;
            int poly2counter = 0;
            int numpolys;
            int i;
            double compConst = 0.000000000001;
            // for debugging 
            int case1 = 0, case2 = 0, case3 = 0, case31 = 0, case32 = 0, case33 = 0, case311 = 0, case3111 = 0;
            // intersect all edges of poly with line
            for (i = 0; i < 2 * numvertices; i = i + 2)
            {
                int j = (i + 2 >= 2 * numvertices) ? 0 : i + 2;
                LineLineSegmentIntersection(x1, y1, x2, y2, convexPoly[i], convexPoly[i + 1], convexPoly[j], convexPoly[j + 1], ref p);
                // if this edge does not intersect with line
                if (Math.Abs(p[0] - 0.0) <= compConst)
                {
                    //System.out.println("null");
                    // add p[j] to the proper polygon
                    if (state == 1)
                    {
                        poly2counter++;
                        poly2[2 * poly2counter - 1] = convexPoly[j];
                        poly2[2 * poly2counter] = convexPoly[j + 1];
                    }
                    else
                    {
                        poly1counter++;
                        poly1[2 * poly1counter - 1] = convexPoly[j];
                        poly1[2 * poly1counter] = convexPoly[j + 1];
                    }
                    // debug
                    case1++;
                }
                // ... or if the intersection is the whole edge
                else if (Math.Abs(p[0] - 2.0) <= compConst)
                {
                    //System.out.println(o);
                    // then we can not reach state 1 and 2
                    poly1counter++;
                    poly1[2 * poly1counter - 1] = convexPoly[j];
                    poly1[2 * poly1counter] = convexPoly[j + 1];
                    // debug
                    case2++;
                }
                // ... or if the intersection is a point
                else
                {
                    // debug
                    case3++;
                    // if the point is the second vertex of the edge
                    if (Math.Abs(p[1] - convexPoly[j]) <= compConst && Math.Abs(p[2] - convexPoly[j + 1]) <= compConst)
                    {
                        // debug
                        case31++;
                        if (state == 1)
                        {
                            poly2counter++;
                            poly2[2 * poly2counter - 1] = convexPoly[j];
                            poly2[2 * poly2counter] = convexPoly[j + 1];
                            poly1counter++;
                            poly1[2 * poly1counter - 1] = convexPoly[j];
                            poly1[2 * poly1counter] = convexPoly[j + 1];
                            state++;
                        }
                        else if (state == 0)
                        {
                            // debug
                            case311++;
                            poly1counter++;
                            poly1[2 * poly1counter - 1] = convexPoly[j];
                            poly1[2 * poly1counter] = convexPoly[j + 1];
                            // test whether the polygon is splitted
                            // or the line only touches the polygon
                            if (i + 4 < 2 * numvertices)
                            {
                                int s1 = LinePointLocation(x1, y1, x2, y2, convexPoly[i], convexPoly[i + 1]);
                                int s2 = LinePointLocation(x1, y1, x2, y2, convexPoly[i + 4], convexPoly[i + 5]);
                                // the line only splits the polygon
                                // when the previous and next vertex lie
                                // on different sides of the line
                                if (s1 != s2 && s1 != 0 && s2 != 0)
                                {
                                    // debug
                                    case3111++;
                                    poly2counter++;
                                    poly2[2 * poly2counter - 1] = convexPoly[j];
                                    poly2[2 * poly2counter] = convexPoly[j + 1];
                                    state++;
                                }
                            }
                        }
                    }
                    // ... if the point is not the other vertex of the edge
                    else if (!(Math.Abs(p[1] - convexPoly[i]) <= compConst && Math.Abs(p[2] - convexPoly[i + 1]) <= compConst))
                    {
                        // debug
                        case32++;
                        poly1counter++;
                        poly1[2 * poly1counter - 1] = p[1];
                        poly1[2 * poly1counter] = p[2];
                        poly2counter++;
                        poly2[2 * poly2counter - 1] = p[1];
                        poly2[2 * poly2counter] = p[2];
                        if (state == 1)
                        {
                            poly1counter++;
                            poly1[2 * poly1counter - 1] = convexPoly[j];
                            poly1[2 * poly1counter] = convexPoly[j + 1];
                        }
                        else if (state == 0)
                        {
                            poly2counter++;
                            poly2[2 * poly2counter - 1] = convexPoly[j];
                            poly2[2 * poly2counter] = convexPoly[j + 1];
                        }
                        state++;
                    }
                    // ... else if the point is the second vertex of the edge
                    else
                    {
                        // debug
                        case33++;
                        if (state == 1)
                        {
                            poly2counter++;
                            poly2[2 * poly2counter - 1] = convexPoly[j];
                            poly2[2 * poly2counter] = convexPoly[j + 1];
                        }
                        else
                        {
                            poly1counter++;
                            poly1[2 * poly1counter - 1] = convexPoly[j];
                            poly1[2 * poly1counter] = convexPoly[j + 1];
                        }
                    }
                }
            }
            // after splitting the state must be 0 or 2
            // (depending whether the polygon was splitted or not)
            if (state != 0 && state != 2)
            {
                // printf("there is something wrong state: %d\n", state);
                // printf("polygon might not be convex!!\n");
                // printf("case1: %d\ncase2: %d\ncase3: %d\ncase31: %d case311: %d case3111: %d\ncase32: %d\ncase33: %d\n", case1, case2, case3, case31, case311, case3111, case32, case33);
                // printf("numvertices %d\n=============\n", numvertices);

                // if there is something wrong with the intersection, just ignore this one				
                numpolys = 3;
            }
            else
            {
                // finally convert the vertex lists into convex polygons
                numpolys = (state == 0) ? 1 : 2;
                poly1[0] = poly1counter;
                poly2[0] = poly2counter;
                // convert the first convex polygon		
                polys[0] = poly1;
                // convert the second convex polygon
                if (state == 2)
                {
                    polys[1] = poly2;
                }
            }
            return numpolys;
        }

        /// <summary>
        /// Determines on which side (relative to the direction) of the given line and the 
        /// point lies (regarding the directional vector) of the given line.
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        /// <remarks>
        /// http://www.mathematik.uni-ulm.de/stochastik/lehre/ws03_04/rt/Geometry2D.ps
        /// </remarks>
        private int LinePointLocation(double x1, double y1, double x2, double y2, double x, double y)
        {
            double z;
            if (Math.Atan((y2 - y1) / (x2 - x1)) * 180.0 / Math.PI == 90.0)
            {
                if (Math.Abs(x1 - x) <= 0.00000000001)
                    return 0;
            }
            else
            {
                if (Math.Abs(y1 + (((y2 - y1) * (x - x1)) / (x2 - x1)) - y) <= EPS)
                    return 0;
            }
            // third component of the 3 dimensional product
            z = (x2 - x1) * (y - y1) - (y2 - y1) * (x - x1);
            if (Math.Abs(z - 0.0) <= 0.00000000001)
            {
                return 0;
            }
            else if (z > 0)
            {
                return 1;
            }
            else
            {
                return 2;
            }
        }

        /// <summary>
        /// Given four points representing one line and a line segment, returns the intersection point
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="x3"></param>
        /// <param name="y3"></param>
        /// <param name="x4"></param>
        /// <param name="y4"></param>
        /// <param name="p"></param>
        /// <remarks>
        /// referenced to: http://local.wasp.uwa.edu.au/~pbourke/geometry/
        /// </remarks>
        private void LineLineSegmentIntersection(
            double x1, double y1,
            double x2, double y2,
            double x3, double y3,
            double x4, double y4, ref double[] p)
        {
            // x1,y1  P1 coordinates (point of line)
            // x2,y2  P2 coordinates (point of line)	
            // x3,y3  P3 coordinates (point of line segment)
            // x4,y4  P4 coordinates (point of line segment)
            // p[1],p[2]   intersection coordinates
            //
            // This function returns a pointer array which first index indicates
            // weather they intersect on one point or not, followed by coordinate pairs.

            double u_a, u_b, denom;
            double compConst = 0.0000000000001;
            // calculate denominator first
            denom = (y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1);
            u_a = (x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3);
            u_b = (x2 - x1) * (y1 - y3) - (y2 - y1) * (x1 - x3);


            //if(fabs(denom-0.0) < compConst && (fabs(u_b-0.0) < compConst && fabs(u_a-0.0) < compConst)){
            //printf("denom %.20f u_b  %.20f u_a  %.20f\n",denom, u_b, u_a);
            if (Math.Abs(denom - 0.0) < compConst)
            {
                if (Math.Abs(u_b - 0.0) < compConst && Math.Abs(u_a - 0.0) < compConst)
                {
                    p[0] = 2.0;	// if denominator and numerator equal to zero, lines are coincident
                }
                else
                {
                    p[0] = 0.0;// if denominator equals to zero, lines are parallel
                }

            }
            else
            {
                u_b = u_b / denom;
                u_a = u_a / denom;
                // 	    printf("u_b %.20f\n", u_b);
                if (u_b < -compConst || u_b > 1.0 + compConst)
                {	// check if it is on the line segment		
                    // 		printf("line (%.20f, %.20f) (%.20f, %.20f) line seg (%.20f, %.20f) (%.20f, %.20f) \n",x1, y1 ,x2, y2 ,x3, y3 , x4, y4);		
                    p[0] = 0.0;
                }
                else
                {
                    p[0] = 1.0;
                    p[1] = x1 + u_a * (x2 - x1); // intersection point
                    p[2] = y1 + u_a * (y2 - y1);
                }
            }

        }

        /// <summary>
        /// Returns the centroid of a given polygon 
        /// </summary>
        /// <param name="numpoints"></param>
        /// <param name="points"></param>
        /// <param name="centroid">Centroid of a given polygon </param>
        private void FindPolyCentroid(int numpoints, double[] points, ref double[] centroid)
        {
            int i;
            //double area = 0.0;//, temp
            centroid[0] = 0.0; centroid[1] = 0.0;

            for (i = 0; i < 2 * numpoints; i = i + 2)
            {

                centroid[0] = centroid[0] + points[i];
                centroid[1] = centroid[1] + points[i + 1];

            }
            centroid[0] = centroid[0] / numpoints;
            centroid[1] = centroid[1] / numpoints;
        }

        /// <summary>
        /// Given two points representing a line and  a radius together with a center point 
        /// representing a circle, returns the intersection points.
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="x3"></param>
        /// <param name="y3"></param>
        /// <param name="r"></param>
        /// <param name="p">Pointer to list of intersection points</param>
        /// <remarks>
        /// referenced to: http://local.wasp.uwa.edu.au/~pbourke/geometry/sphereline/
        /// </remarks>
        private void CircleLineIntersection(
            double x1, double y1,
            double x2, double y2,
            double x3, double y3, double r, ref double[] p)
        {
            // x1,y1  P1 coordinates [point of line]
            // x2,y2  P2 coordinates [point of line]
            // x3,y3, r  P3 coordinates(circle center) and radius [circle]	
            // p[1],p[2]; p[3],p[4]   intersection coordinates
            //
            // This function returns a pointer array which first index indicates
            // the number of intersection points, followed by coordinate pairs.

            //double x , y ;
            double a, b, c, mu, i;

            a = (x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1);
            b = 2 * ((x2 - x1) * (x1 - x3) + (y2 - y1) * (y1 - y3));
            c = x3 * x3 + y3 * y3 + x1 * x1 + y1 * y1 - 2 * (x3 * x1 + y3 * y1) - r * r;
            i = b * b - 4 * a * c;

            if (i < 0.0)
            {
                // no intersection
                p[0] = 0.0;
            }
            else if (Math.Abs(i - 0.0) < EPS)
            {
                // one intersection
                p[0] = 1.0;

                mu = -b / (2 * a);
                p[1] = x1 + mu * (x2 - x1);
                p[2] = y1 + mu * (y2 - y1);

            }
            else if (i > 0.0 && !(Math.Abs(a - 0.0) < EPS))
            {
                // two intersections
                p[0] = 2.0;
                // first intersection
                mu = (-b + Math.Sqrt(i)) / (2 * a);
                p[1] = x1 + mu * (x2 - x1);
                p[2] = y1 + mu * (y2 - y1);
                // second intersection
                mu = (-b - Math.Sqrt(i)) / (2 * a);
                p[3] = x1 + mu * (x2 - x1);
                p[4] = y1 + mu * (y2 - y1);


            }
            else
            {
                p[0] = 0.0;
            }
        }

        /// <summary>
        /// Given three points, check if the point is the correct point that we are looking for.
        /// </summary>
        /// <param name="x1">P1 coordinates (bisector point of dual edge on triangle)</param>
        /// <param name="y1">P1 coordinates (bisector point of dual edge on triangle)</param>
        /// <param name="x2">P2 coordinates (intersection point)</param>
        /// <param name="y2">P2 coordinates (intersection point)</param>
        /// <param name="x3">P3 coordinates (circumcenter point)</param>
        /// <param name="y3">P3 coordinates (circumcenter point)</param>
        /// <param name="isObtuse"></param>
        /// <returns>Returns true, if given point is the correct one otherwise return false.</returns>
        private bool ChooseCorrectPoint(
            double x1, double y1,
            double x2, double y2,
            double x3, double y3, bool isObtuse)
        {
            double d1, d2;
            bool p;

            // squared distance between circumcenter and intersection point
            d1 = (x2 - x3) * (x2 - x3) + (y2 - y3) * (y2 - y3);
            // squared distance between bisector point and intersection point
            d2 = (x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1);

            if (isObtuse)
            {
                // obtuse case
                if (d2 >= d1)
                {
                    p = true; // means we have found the right point
                }
                else
                {
                    p = false; // means take the other point
                }
            }
            else
            {
                // non-obtuse case
                if (d2 < d1)
                {
                    p = true; // means we have found the right point
                }
                else
                {
                    p = false; // means take the other point
                }
            }
            /// HANDLE RIGHT TRIANGLE CASE!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            return p;

        }

        /// <summary>
        /// This function returns a pointer array which first index indicates the whether 
        /// the point is in between the other points, followed by coordinate pairs.
        /// </summary>
        /// <param name="x1">P1 coordinates [point of line] (point on Voronoi edge - intersection)</param>
        /// <param name="y1">P1 coordinates [point of line] (point on Voronoi edge - intersection)</param>
        /// <param name="x2">P2 coordinates [point of line] (circumcenter)</param>
        /// <param name="y2">P2 coordinates [point of line] (circumcenter)</param>
        /// <param name="x">P3 coordinates [point to be compared]	(neighbor's circumcenter)</param>
        /// <param name="y">P3 coordinates [point to be compared]	(neighbor's circumcenter)</param>
        /// <param name="p"></param>
        private void PointBetweenPoints(double x1, double y1, double x2, double y2, double x, double y, ref double[] p)
        {
            // now check whether the point is close to circumcenter than intersection point
            // BETWEEN THE POINTS
            if ((x2 - x) * (x2 - x) + (y2 - y) * (y2 - y) < (x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1))
            {
                p[0] = 1.0;
                // calculate the squared distance to circumcenter
                p[1] = (x - x2) * (x - x2) + (y - y2) * (y - y2);
                p[2] = x;
                p[3] = y;
            }// *NOT* BETWEEN THE POINTS
            else
            {
                p[0] = 0.0;
                p[1] = 0.0;
                p[2] = 0.0;
                p[3] = 0.0;
            }
        }

        /// <summary>
        /// Given three coordinates of a triangle, tests a triangle to see if it satisfies 
        /// the minimum and/or maximum angle condition.
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="x3"></param>
        /// <param name="y3"></param>
        /// <returns>Returns true, if it is a BAD triangle, returns false if it is a GOOD triangle.</returns>
        private bool IsBadTriangleAngle(double x1, double y1, double x2, double y2, double x3, double y3)
        {
            // variables keeping the distance values for the edges
            double dxod, dyod, dxda, dyda, dxao, dyao;
            double dxod2, dyod2, dxda2, dyda2, dxao2, dyao2;

            double apexlen, orglen, destlen;
            double angle;    // in order to check minimum angle condition 

            double maxangle;    // in order to check minimum angle condition
            // calculate the side lengths

            dxod = x1 - x2;
            dyod = y1 - y2;
            dxda = x2 - x3;
            dyda = y2 - y3;
            dxao = x3 - x1;
            dyao = y3 - y1;
            // calculate the squares of the side lentghs
            dxod2 = dxod * dxod;
            dyod2 = dyod * dyod;
            dxda2 = dxda * dxda;
            dyda2 = dyda * dyda;
            dxao2 = dxao * dxao;
            dyao2 = dyao * dyao;

            // Find the lengths of the triangle's three edges.
            apexlen = dxod2 + dyod2;
            orglen = dxda2 + dyda2;
            destlen = dxao2 + dyao2;

            // try to find the minimum edge and accordingly the pqr orientation
            if ((apexlen < orglen) && (apexlen < destlen))
            {
                // Find the square of the cosine of the angle at the apex.
                angle = dxda * dxao + dyda * dyao;
                angle = angle * angle / (orglen * destlen);
            }
            else if (orglen < destlen)
            {
                // Find the square of the cosine of the angle at the origin.
                angle = dxod * dxao + dyod * dyao;
                angle = angle * angle / (apexlen * destlen);
            }
            else
            {
                // Find the square of the cosine of the angle at the destination.
                angle = dxod * dxda + dyod * dyda;
                angle = angle * angle / (apexlen * orglen);

            }

            // try to find the maximum edge and accordingly the pqr orientation
            if ((apexlen > orglen) && (apexlen > destlen))
            {
                // Find the cosine of the angle at the apex.
                maxangle = (orglen + destlen - apexlen) / (2 * Math.Sqrt(orglen * destlen));
            }
            else if (orglen > destlen)
            {
                // Find the cosine of the angle at the origin.
                maxangle = (apexlen + destlen - orglen) / (2 * Math.Sqrt(apexlen * destlen));
            }
            else
            {
                // Find the cosine of the angle at the destination.
                maxangle = (apexlen + orglen - destlen) / (2 * Math.Sqrt(apexlen * orglen));
            }

            // Check whether the angle is smaller than permitted.
            if ((angle > behavior.goodAngle) || (behavior.MaxAngle != 0.00 && maxangle < behavior.maxGoodAngle))
            {
                return true;// it is a bad triangle
            }

            return false;// it is a good triangle
        }

        /// <summary>
        /// Given the triangulation, and a vertex returns the minimum distance to the 
        /// vertices of the triangle where the given vertex located.
        /// </summary>
        /// <param name="newlocX"></param>
        /// <param name="newlocY"></param>
        /// <param name="searchtri"></param>
        /// <returns></returns>
        private double MinDistanceToNeighbor(double newlocX, double newlocY, ref Otri searchtri)
        {
            Otri horiz = default(Otri);	// for search operation
            LocateResult intersect = LocateResult.Outside;
            Vertex v1, v2, v3, torg, tdest;
            double d1, d2, d3, ahead;
            //triangle ptr;                         // Temporary variable used by sym().

            Point newvertex = new Point(newlocX, newlocY);

            // 	printf("newvertex %f,%f\n", newvertex[0], newvertex[1]);
            // Find the location of the vertex to be inserted.  Check if a good
            //   starting triangle has already been provided by the caller.	
            // Find a boundary triangle.
            //horiz.tri = m.dummytri;
            //horiz.orient = 0;
            //horiz.symself();
            // Search for a triangle containing 'newvertex'.
            // Start searching from the triangle provided by the caller.
            // Where are we?
            torg = searchtri.Org();
            tdest = searchtri.Dest();
            // Check the starting triangle's vertices.
            if ((torg.x == newvertex.x) && (torg.y == newvertex.y))
            {
                intersect = LocateResult.OnVertex;
                searchtri.Copy(ref horiz);

            }
            else if ((tdest.x == newvertex.x) && (tdest.y == newvertex.y))
            {
                searchtri.Lnext();
                intersect = LocateResult.OnVertex;
                searchtri.Copy(ref horiz);
            }
            else
            {
                // Orient 'searchtri' to fit the preconditions of calling preciselocate().
                ahead = predicates.CounterClockwise(torg, tdest, newvertex);
                if (ahead < 0.0)
                {
                    // Turn around so that 'searchpoint' is to the left of the
                    // edge specified by 'searchtri'.
                    searchtri.Sym();
                    searchtri.Copy(ref horiz);
                    intersect = mesh.locator.PreciseLocate(newvertex, ref horiz, false);
                }
                else if (ahead == 0.0)
                {
                    // Check if 'searchpoint' is between 'torg' and 'tdest'.
                    if (((torg.x < newvertex.x) == (newvertex.x < tdest.x)) &&
                        ((torg.y < newvertex.y) == (newvertex.y < tdest.y)))
                    {
                        intersect = LocateResult.OnEdge;
                        searchtri.Copy(ref horiz);

                    }
                }
                else
                {
                    searchtri.Copy(ref horiz);
                    intersect = mesh.locator.PreciseLocate(newvertex, ref horiz, false);
                }
            }
            if (intersect == LocateResult.OnVertex || intersect == LocateResult.Outside)
            {
                // set distance to 0
                //m.VertexDealloc(newvertex);
                return 0.0;
            }
            else
            { // intersect == ONEDGE || intersect == INTRIANGLE
                // find the triangle vertices
                v1 = horiz.Org();
                v2 = horiz.Dest();
                v3 = horiz.Apex();
                d1 = (v1.x - newvertex.x) * (v1.x - newvertex.x) + (v1.y - newvertex.y) * (v1.y - newvertex.y);
                d2 = (v2.x - newvertex.x) * (v2.x - newvertex.x) + (v2.y - newvertex.y) * (v2.y - newvertex.y);
                d3 = (v3.x - newvertex.x) * (v3.x - newvertex.x) + (v3.y - newvertex.y) * (v3.y - newvertex.y);
                //m.VertexDealloc(newvertex);
                // find minimum of the distance
                if (d1 <= d2 && d1 <= d3)
                {
                    return d1;
                }
                else if (d2 <= d3)
                {
                    return d2;
                }
                else
                {
                    return d3;
                }
            }
        }
    }
}
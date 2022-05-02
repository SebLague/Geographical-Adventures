//MIT License

//Copyright(c) 2019 Antony Vitillo(a.k.a. "Skarredghost")

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using UnityEngine;
using System.Collections;
using TMPro;

namespace ntw.CurvedTextMeshPro
{
    /// <summary>
    /// Class for drawing a Text Pro text following a n^x function
    /// </summary>
    [ExecuteInEditMode]
    public class TextProOnAExp : TextProOnACurve
    {
        /// <summary>
        /// The base of the exponential curve
        /// </summary>
        [SerializeField]
        [Tooltip("The base of the exponential curve")]
        private float m_expBase = 1.3f;

        /// <summary>
        /// Previous value of <see cref="m_expBase"/>
        /// </summary>
        private float m_oldExpBase = float.MaxValue;
    
        /// <summary>
        /// Method executed at every frame that checks if some parameters have been changed
        /// </summary>
        /// <returns></returns>
        protected override bool ParametersHaveChanged()
        {
            //check if paramters have changed and update the old values for next frame iteration
            bool retVal = m_expBase != m_oldExpBase;

            m_oldExpBase = m_expBase;
           
            return retVal;
        }

        /// <summary>
        /// Computes the transformation matrix that maps the offsets of the vertices of each single character from
        /// the character's center to the final destinations of the vertices so that the text follows a curve
        /// </summary>
        /// <param name="charMidBaselinePosfloat">Position of the central point of the character</param>
        /// <param name="zeroToOnePos">Horizontal position of the character relative to the bounds of the box, in a range [0, 1]</param>
        /// <param name="textInfo">Information on the text that we are showing</param>
        /// <param name="charIdx">Index of the character we have to compute the transformation for</param>
        /// <returns>Transformation matrix to be applied to all vertices of the text</returns>
        protected override Matrix4x4 ComputeTransformationMatrix(Vector3 charMidBaselinePos, float zeroToOnePos, TMP_TextInfo textInfo, int charIdx)
        {        
            //compute the coordinates of the new position of the central point of the character. Use the exp function
            //Notice that we have to do some extra calculations because we have to take in count that text may be on multiple lines
            float x0 = charMidBaselinePos.x;
            float y0 = Mathf.Pow(m_expBase, x0);
            Vector2 newMideBaselinePos = new Vector2(x0, y0 - textInfo.lineInfo[0].lineExtents.max.y * textInfo.characterInfo[charIdx].lineNumber); //actual new position of the character

            //compute the trasformation matrix: move the points to the just found position, then rotate the character to fit the angle of the curve 
            //(I do some calculations using derivative of the exp function for the orientation)
            return Matrix4x4.TRS(new Vector3(newMideBaselinePos.x, newMideBaselinePos.y, 0), Quaternion.AngleAxis(Mathf.Atan(Mathf.Log(m_expBase) * y0) * Mathf.Rad2Deg, Vector3.forward), Vector3.one);
        }
    }
}

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

//Code inspired by the one of the WarpTextExample of TextMeshPro package

using UnityEngine;
using System.Collections;
using TMPro;
using System;

namespace ntw.CurvedTextMeshPro
{
    /// <summary>
    /// Base class for drawing a Text Pro text following a particular curve
    /// </summary>
    [ExecuteInEditMode]
    public abstract class TextProOnACurve : MonoBehaviour
    {
        /// <summary>
        /// The text component of interest
        /// </summary>
        private TMP_Text m_TextComponent;

        /// <summary>
        /// True if the text must be updated at this frame 
        /// </summary>
        private bool m_forceUpdate;

        /// <summary>
        /// Awake
        /// </summary>
        private void Awake()
        {
            m_TextComponent = gameObject.GetComponent<TMP_Text>();
        }

        /// <summary>
        /// OnEnable
        /// </summary>
        private void OnEnable()
        {
            //every time the object gets enabled, we have to force a re-creation of the text mesh
            m_forceUpdate = true;
        }

        /// <summary>
        /// Update
        /// </summary>
        protected void Update()
        {
            //if the text and the parameters are the same of the old frame, don't waste time in re-computing everything
            if (!m_forceUpdate && !m_TextComponent.havePropertiesChanged && !ParametersHaveChanged())
            {
                return;
            }

            m_forceUpdate = false;

            //during the loop, vertices represents the 4 vertices of a single character we're analyzing, 
            //while matrix is the roto-translation matrix that will rotate and scale the characters so that they will
            //follow the curve
            Vector3[] vertices;
            Matrix4x4 matrix;

            //Generate the mesh and get information about the text and the characters
            m_TextComponent.ForceMeshUpdate();

            TMP_TextInfo textInfo = m_TextComponent.textInfo;
            int characterCount = textInfo.characterCount;

            //if the string is empty, no need to waste time
            if (characterCount == 0)
                return;

            //gets the bounds of the rectangle that contains the text 
            float boundsMinX = m_TextComponent.bounds.min.x;
            float boundsMaxX = m_TextComponent.bounds.max.x;

            //for each character
            for (int i = 0; i < characterCount; i++)
            {
                //skip if it is invisible
                if (!textInfo.characterInfo[i].isVisible)
                    continue;

                //Get the index of the mesh used by this character, then the one of the material... and use all this data to get
                //the 4 vertices of the rect that encloses this character. Store them in vertices
                int vertexIndex = textInfo.characterInfo[i].vertexIndex;
                int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
                vertices = textInfo.meshInfo[materialIndex].vertices;

                //Compute the baseline mid point for each character. This is the central point of the character.
                //we will use this as the point representing this character for the geometry transformations
                Vector3 charMidBaselinePos = new Vector2((vertices[vertexIndex + 0].x + vertices[vertexIndex + 2].x) / 2, textInfo.characterInfo[i].baseLine);

                //remove the central point from the vertices point. After this operation, every one of the four vertices 
                //will just have as coordinates the offset from the central position. This will come handy when will deal with the rotations
                vertices[vertexIndex + 0] += -charMidBaselinePos;
                vertices[vertexIndex + 1] += -charMidBaselinePos;
                vertices[vertexIndex + 2] += -charMidBaselinePos;
                vertices[vertexIndex + 3] += -charMidBaselinePos;

                //compute the horizontal position of the character relative to the bounds of the box, in a range [0, 1]
                //where 0 is the left border of the text and 1 is the right border
                float zeroToOnePos = (charMidBaselinePos.x - boundsMinX) / (boundsMaxX - boundsMinX);

                //get the transformation matrix, that maps the vertices, seen as offset from the central character point, to their final
                //position that follows the curve
                matrix = ComputeTransformationMatrix(charMidBaselinePos, zeroToOnePos, textInfo, i);

                //apply the transformation, and obtain the final position and orientation of the 4 vertices representing this char
                vertices[vertexIndex + 0] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 0]);
                vertices[vertexIndex + 1] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 1]);
                vertices[vertexIndex + 2] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 2]);
                vertices[vertexIndex + 3] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 3]);
            }

            //Upload the mesh with the revised information
            m_TextComponent.UpdateVertexData();
        }

        /// <summary>
        /// Method executed at every frame that checks if some parameters have been changed
        /// </summary>
        /// <returns></returns>
        protected abstract bool ParametersHaveChanged();

        /// <summary>
        /// Computes the transformation matrix that maps the offsets of the vertices of each single character from
        /// the character's center to the final destinations of the vertices so that the text follows a curve
        /// </summary>
        /// <param name="charMidBaselinePosfloat">Position of the central point of the character</param>
        /// <param name="zeroToOnePos">Horizontal position of the character relative to the bounds of the box, in a range [0, 1]</param>
        /// <param name="textInfo">Information on the text that we are showing</param>
        /// <param name="charIdx">Index of the character we have to compute the transformation for</param>
        /// <returns>Transformation matrix to be applied to all vertices of the text</returns>
        protected abstract Matrix4x4 ComputeTransformationMatrix(Vector3 charMidBaselinePos, float zeroToOnePos, TMP_TextInfo textInfo, int charIdx);
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Compass : MonoBehaviour
{

    public GameCamera gameCamera;
    public Transform player;
    public Transform plane;
    public RectTransform dial;
    public RectTransform headingUI;
    public RectTransform altitudeIndicator;
    public CoordinateDegrees northMagneticPole;



    void Start()
    {
        gameCamera.gameCameraUpdateComplete += (cam) => UpdateCompass();
    }

    void UpdateCompass()
    {
        Vector3 northDir = CalculateNorth(player.position);
        float angle = Vector3.SignedAngle(player.forward, northDir, -player.up);


        if (gameCamera.topDownMode)
        {
            float playerAngleRelativeToCamera = Vector3.SignedAngle(gameCamera.transform.up, player.forward, -player.up);
            headingUI.eulerAngles = Vector3.forward * playerAngleRelativeToCamera;
            dial.eulerAngles = Vector3.forward * (angle + playerAngleRelativeToCamera);
        }
        else
        {

            //Update Altitude Indicator
            altitudeIndicator.transform.localPosition = new Vector3(0, plane.localEulerAngles.x < 319 ? (plane.localEulerAngles.x / 2) - 105 : (plane.localEulerAngles.x / 2) - 285, 0);
            altitudeIndicator.transform.eulerAngles = new Vector3(0, 0, -plane.localEulerAngles.z / 2);

            // Todo: actually calculate based on cam pos relative to player to handle more exotic views that might be added later...
            if (gameCamera.activeView == GameCamera.ViewMode.LookingForward || gameCamera.activeView == GameCamera.ViewMode.MainMenu)
            {
                headingUI.eulerAngles = Vector3.forward * 0;
                dial.eulerAngles = Vector3.forward * angle;
            }
            else if (gameCamera.activeView == GameCamera.ViewMode.LookingBehind)
            {
                headingUI.eulerAngles = Vector3.forward * 180;
                dial.eulerAngles = Vector3.forward * (angle + 180);
            }
        }
    }

    Vector3 CalculateNorth(Vector3 pos)
    {
        pos = pos.normalized;
        Vector3 posNorth = GeoMaths.CoordinateToPoint(northMagneticPole.ConvertToRadians());
        Vector3 greatCircleNormal = Vector3.Cross(posNorth, pos);
        return Vector3.Cross(pos, greatCircleNormal).normalized;
    }

}

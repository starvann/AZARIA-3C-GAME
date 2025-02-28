using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering.LookDev;
using UnityEngine;
using Cinemachine;

public class CameraManager : MonoBehaviour
{
    [SerializeField]
    private CinemachineFreeLook _tpsCamera;
    [SerializeField]
    public CameraState CameraState;

    [SerializeField]
    private CinemachineVirtualCamera _fpsCamera;

    [SerializeField]
    private InputManager _inputManager;

    private void Start()
    {
        _inputManager.OnChangePOV += SwitchCamera;
    }

    private void OnDestroy()
    {
        _inputManager.OnChangePOV -= SwitchCamera;
    }

    private void SwitchCamera()
    {
        if (CameraState == CameraState.ThirdPerson)
        {
            CameraState = CameraState.FirstPerson;
            _tpsCamera.gameObject.SetActive(false);
            _fpsCamera.gameObject.SetActive(true);
        }
        else
        {
            CameraState = CameraState.ThirdPerson; 
            _tpsCamera.gameObject.SetActive(true);
            _tpsCamera.gameObject.SetActive(false);
        }
    }
    public void SetFPSClampedCamera(bool isClamped, Vector3 playerRotation)
    {
        CinemachinePOV pov = _fpsCamera.GetCinemachineComponent<CinemachinePOV>();
        if (isClamped)
        {
            pov.m_HorizontalAxis.m_Wrap = false;
            pov.m_HorizontalAxis.m_MinValue = playerRotation.y - 45;
            pov.m_HorizontalAxis.m_MaxValue = playerRotation.y + 45;
        }
        else
        {
            pov.m_HorizontalAxis.m_MinValue = -180;
            pov.m_HorizontalAxis.m_MaxValue = 180;
            pov.m_HorizontalAxis.m_Wrap = true;
        }
    }

    public void SetTPSFieldOfView(float fieldOfView)
    {
        _tpsCamera.m_Lens.FieldOfView = fieldOfView;
    }

}

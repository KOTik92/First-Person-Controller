using UnityEngine;

public static class Inputs
{
    public static bool IsLeftShift => Input.GetKey(KeyCode.LeftShift);
    public static bool IsEscape => Input.GetKeyDown(KeyCode.Escape);
    public static bool IsLeftControl => Input.GetKeyDown(KeyCode.LeftControl);
    public static bool IsSpace => Input.GetKeyDown(KeyCode.Space);
    public static bool IsE => Input.GetKey(KeyCode.E);

    public static float MouseScrollWheel => Input.GetAxis("Mouse ScrollWheel");
    public static float MouseX => Input.GetAxis("Mouse X");
    public static float MouseY => Input.GetAxis("Mouse Y");
    public static float Horizontal => Input.GetAxis("Horizontal");
    public static float Vertical => Input.GetAxis("Vertical");
    
    public static bool[] IsNumber => new[]
    {
        Input.GetKeyDown(KeyCode.Alpha1), 
        Input.GetKeyDown(KeyCode.Alpha2),
        Input.GetKeyDown(KeyCode.Alpha3),
        Input.GetKeyDown(KeyCode.Alpha4),
        Input.GetKeyDown(KeyCode.Alpha5),
        Input.GetKeyDown(KeyCode.Alpha6),
        Input.GetKeyDown(KeyCode.Alpha7),
        Input.GetKeyDown(KeyCode.Alpha8),
        Input.GetKeyDown(KeyCode.Alpha9),
    };

}

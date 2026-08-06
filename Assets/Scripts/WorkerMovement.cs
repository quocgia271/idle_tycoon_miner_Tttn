using UnityEngine;
using System;

public class WorkerMovement : MonoBehaviour
{
    public bool flipRotation = false; // Đánh dấu true nếu nhân vật quay mặt ngược hướng mặc định

    private Vector3 targetPosition;
    private bool isMoving = false;
    private float currentSpeed;
    private Action onDestinationReached;
    
    // Giữ lại cờ để Worker Animation hoặc UI có thể kiểm tra hướng
    public bool isFlipped => transform.rotation.eulerAngles.y > 90f;

    public void MoveTo(Vector3 target, float speed, Action onReached = null)
    {
        targetPosition = new Vector3(target.x, transform.position.y, transform.position.z);
        currentSpeed = speed;
        onDestinationReached = onReached;
        isMoving = true;
        
        // Tính toán hướng quay
        float yRight = flipRotation ? 0f : 180f;
        float yLeft = flipRotation ? 180f : 0f;

        float direction = targetPosition.x - transform.position.x;
        if (direction > 0.01f) // Đi sang phải
        {
            transform.rotation = Quaternion.Euler(0, yRight, 0);
        }
        else if (direction < -0.01f) // Đi sang trái
        {
            transform.rotation = Quaternion.Euler(0, yLeft, 0);
        }
    }

    public void StopMoving()
    {
        isMoving = false;
        onDestinationReached = null;
    }

    private void Update()
    {
        if (!isMoving) return;

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, currentSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
        {
            isMoving = false;
            onDestinationReached?.Invoke();
            onDestinationReached = null;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class eventList : MonoBehaviour
{
    public GameObject imagePrefab; // Image 프리팹
    public Transform eventImagePanel;   // 부모 패널
    private List<Sprite> sprites = new List<Sprite>(); // 스프라이트 리스트로 변경

    public static eventList Instance { get; private set; }

    public void AddSprite(Sprite sprite) // 스프라이트 추가
    {
        if (sprite != null)
        {
            sprites.Add(sprite);
        }
    }

    public void ClearSprite()
    {
        foreach (Transform child in eventImagePanel)
        {
            Destroy(child.gameObject); // 모든 자식 오브젝트 삭제
        }
    }

    public void PopulateImages()
    {
        ClearSprite(); // 기존 자식 정리

        foreach (Sprite sprite in sprites)
        {
            if (sprite == null) continue; // null 값 무시
            // 이미지 생성
            GameObject newImage = Instantiate(imagePrefab, eventImagePanel);

            // 스프라이트 적용
            Image imageComponent = newImage.GetComponent<Image>();
            if (imageComponent != null)
            {
                imageComponent.sprite = sprite;
            }
        }
    }

    private void Awake() // Unity의 Awake() 메서드에서 싱글톤 설정
    {
        // 만약 Instance가 비어 있으면, 이 스크립트를 싱글톤 인스턴스로 설정
        if (Instance == null)
        {
            Instance = this; // 현재 객체를 싱글톤으로 지정
        }
        else
        {
            // 이미 싱글톤 인스턴스가 존재하면, 중복된 객체를 제거
            Destroy(gameObject);
        }
    }
    private void Start()
    {
        AddSprite(Resources.Load<Sprite>("button1")); // Resources/button1.png
        AddSprite(Resources.Load<Sprite>("button2")); // Resources/button2.png
        AddSprite(Resources.Load<Sprite>("button3")); // Resources/button3.png
        AddSprite(Resources.Load<Sprite>("button3")); // Resources/button4.png
    }
}

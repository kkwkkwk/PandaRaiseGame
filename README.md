# 판다키우기 방치형 게임
-------------------------------

### 프로젝트 소개
- 판다를 주인공으로 내세운 모바일 방치형 게임 제작을 목표로 Unity 기반 어플리케이션 제작 진행중에 있습니다.
--------------------------------

### 프로젝트 개발 기간
- 2024.12.20 ~ 2025.07.xx
------------------------------------------

### 프로젝트 구조
	/Scripts
		/bottomMenu
		/chat
		/LeftMenu
		/LoginScene
		/PlayFab
		/RightMenu
 
-------------------------------------------------------

### 프로젝트 설명


	/Scripts/bottomMenu : 하단 메뉴 관련 기능을 담당	
  		/dungeon
		/equipment
		/farm
		/skill
			ClosePopup.cs : 팝업을 닫는 기능
			OpenPopup.cs : 팝업을 여는 기능
			PopupManager.cs : 팝업 UI 관리 및 데이터 연동
			PopupRaycaster.cs : 팝업에서 UI 클릭 이벤트를 관리하여 상위 패널로 이벤트가 전달되지 않도록 하는 기능
   
  		/shop
    		OpenShopScene.cs : 상점 씬을 여는 기능
    		PurchaseItemByGold.cs : 유저가 골드를 사용하여 아이템을 구매하는 기능

	/Scripts/chat : 게임 내 채팅 시스템 관련 스크립트 
  		ChatPopupManager.cs : 채팅 UI 팝업 관리
 		CloseChatPopup.cs : 채팅 팝업을 닫는 기능

	/Scripts/LeftMenu : 왼쪽 UI 메뉴 및 퀘스트 관련 기능
 	
  		/dailyLogin
   		/guild
   		/package
   		/quest
    		ClosePopup.cs : 팝업을 닫는 기능
			OpenPopup.cs : 팝업을 여는 기능
			PopupManager.cs : 팝업 UI 관리 및 데이터 연동
			PopupRaycaster.cs : 팝업에서 UI 클릭 이벤트를 관리하여 상위 패널로 이벤트가 전달되지 않도록 하는 기능 
  
  		/profile
    		LoadProfileData.cs : 기능 미구현, 추후 업로드 예정
 
  		/FixName
      		ChangeName.cs : 유저 닉네임 변경 기능
      		FixNamePopupManager.cs : 닉네임 변경 팝업 UI 관리
      		FixNamePopupRaycaster.cs : 닉네임 변경 팝업 UI 클릭 이벤트 관리

	/Scripts/LoginScene : 추후 업로드 예정

	/Scripts/PlayFab : PlayFab을 활용한 로그인, 유저 데이터 관리 및 랭킹 기능
  		PlayFabUserDataManager.cs : 유저의 기본 데이터 관리, 출석 보상 처리
  		AddCurrency.cs : 유저에게 골드를 지급하는 기능
  		
  		/LeaderBoard
    		RankManager.cs : PlayFab을 활용한 유저 랭킹 관리
       
  		/Login
    		LoginPlayFabUser.cs : PlayFab 로그인 및 유저 데이터 확인

	/Scripts/RightMenu : 오른쪽 UI 메뉴 및 길드, 패키지 관련 기능
  		/leaderboard
  		/rewardBox
  		/setting
    		ClosePopup.cs : 팝업을 닫는 기능
			OpenPopup.cs : 팝업을 여는 기능
			PopupManager.cs : 팝업 UI 관리 및 데이터 연동
			PopupRaycaster.cs : 팝업에서 UI 클릭 이벤트를 관리하여 상위 패널로 이벤트가 전달되지 않도록 하는 기능 

  		/event
			eventList.cs : 이벤트 리스트 출력

  

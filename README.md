# 劍靈Neo DPS插件

這個專案根據[原作者](https://github.com/azuradara/neo-act-plugin)開發的抓取方式進行中文化支援，旨在支援台服劍靈NEO，可能有不少BUG跟計算不完整僅供參考且無UI介面顯示，更多開發介紹請參考原作者

>[!警告]
>這個插件讀取遊戲記憶體資料，已違反官方遊戲管理規範，自行斟酌風險使用

### 安裝方式
1. 下載[ACT](https://advancedcombattracker.com/download.php)，並完成安裝
2. 下載[最新版插件](https://github.com/Emagu/neo-act-plugin/releases/latest)
3. 解壓縮並覆蓋掉原本下載的插件(如果下載過的話)，記得先關閉ACT如果有在運行的話
以下為初次安裝所需
4. 使用工作管理員開啟ACT
5. 於上方列表找到Plugins,並點開
6. 於Plugin Listing中找到Browse,並點開
7. 找到剛剛下載並解壓縮的插件，選擇NeoActPlugin.dll
8. 回到ACT的Plugins中應該就會顯示了，確認enabled有勾選
9. [安裝影片Youtube](https://www.youtube.com/watch?v=deu13IIWQys)

### 使用方式
1. ~~每次開啟ACT都需要到Plugins>NeoActPlugin.dll頁面中，有一個下拉選單預設為Global，將它改為Taiwan才能正常抓到台服程式~~
2. 戰鬥資訊只能在Main中查看，因為我懶得改原作者的網頁呈現(其實是看不懂)
3. 戰鬥解析僅在隱藏隊友(Ctrl+F兩次)情況下測試，不確定顯示隊友時統計會不會出錯。隱藏隊友統計不會計算隊友暴率，但是傷害87%準，測出來的傷害會比實際低一點點

### 已知問題
1. 刺客中毒/召喚自然毒/氣功火種/所有出血，這種持續傷害因為分不清楚來源，所以不會列入計算(自己的也不會)以示公平
2. 真言珠(平道/張宿)的額外傷害不會列入計算
3. 台服中文版戰鬥日誌似乎好像沒有攻擊閃避紀錄，所以無法計算命中率
4. 隱藏人物時看不到隊友的攻擊是否暴擊，所以不會統計暴擊率
5. 沒有浮動視窗GUI，單螢幕不能邊打遊戲邊看(我覺得不是缺點，打副本給我認真阿)

## License

MIT License, see [LICENSE](LICENSE) for more information.

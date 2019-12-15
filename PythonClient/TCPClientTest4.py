# クライアントを作成

import sys
import socket
import struct #数値→バイト列変換用
import time

DISP_ID = 0

with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as TCP_SOCKET:
	TCP_SND_IP   = '127.0.0.1'
	TCP_SND_PORT = 60000
	TCP_RCV_PORT = 60000

	with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as UDP_SOCKET:
		UDP_RCV_PORT = 50000
		UDP_RCV_BUFF = UDP_SOCKET.getsockopt(socket.SOL_SOCKET, socket.SO_RCVBUF)

		# サーバを指定
		TCP_SOCKET.connect((TCP_SND_IP, TCP_SND_PORT))
		# サーバにメッセージを送る
		# メッセージ区分０(1byte)
		# ディスプレイID(1byte)
		# TCP受信ポート番号（2byte）
		# UDP受信ポート番号（2byte）
		# UDP受信バッファサイズ（4byte）
		print(UDP_RCV_BUFF)
		s = struct.Struct('<BBHHI')
		SNDDATA = s.pack( 0, DISP_ID, TCP_RCV_PORT, UDP_RCV_PORT, UDP_RCV_BUFF)
		print(SNDDATA)
		print(s.unpack(SNDDATA))
		TCP_SOCKET.send(SNDDATA)
		
		UDP_RCV_BUFF = int.from_bytes(TCP_SOCKET.recv(5), sys.byteorder)
		print(UDP_RCV_BUFF)

		
		# タイムアウト値を設定
		TCP_SOCKET.settimeout(10)
		

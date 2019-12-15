# クライアントを作成

import sys
import socket
import struct #数値→バイト列変換用
import time
import threading

DISP_ID = 0

# サーバIP
TCP_SND_IP   = '127.0.0.1'
# サーバポート
TCP_SND_PORT = 60000

# 生存監視受信ポート
TCP_RCV_PORT = 60001
# 画像受信ポート
UDP_RCV_PORT = 50000
# 画像受信バッファサイズ
UDP_RCV_BUFF = 0
# 画像サイズ
UDP_RCV_SIZE = 0

# TCPサーバクラス
# 初期化応答の受信、生存監視への返答、終了通知の受信を行う
class TCP_SERVER(threading.Thread):
    # 初期化処理
    def __init__(self, PORT):
        threading.Thread.__init__(self)
        self.kill_flag = False

        self.HOST = socket.gethostname()
        self.PORT = PORT
        self.BUFSIZE = 1024


    # 受信待ち
    def run(self):
        # TCPサーバ開始
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as self.TCP_SOCKET:
             self.TCP_SOCKET.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
             self.TCP_SOCKET.settimeout(60)
             try:
                 self.TCP_SOCKET.bind((TCP_SND_IP, self.PORT))
                 self.TCP_SOCKET.listen(5)
                 print('TCPServerStart')
                 # 終了通知が来るまでループ
                 while self.kill_flag == False :
                     (connection, client) = self.TCP_SOCKET.accept()
                     # 先頭１バイト（通信種別）を取得
                     print('Client connected', client)
                     RCV_TYPE = connection.recv(1)
                     print('RECV Type', RCV_TYPE)
                     
                     # 0（UDP初期化処理）
                     if RCV_TYPE == b'\x00':
                         RCVDATA = []
                         bytes_recd = 0
                         # 受信済サイズが４バイトになるまで受信
                         MSGLEN = 4
                         while bytes_recd < MSGLEN:
                             chunk = connection.recv(MSGLEN - bytes_recd)
                             # 受信バイトが空だったら例外を投げる
                             if chunk == b'':
                                 raise RuntimeError("socket connection broken")
                             # 受信データ配列の後ろに追加
                             RCVDATA.append(chunk)
                             bytes_recd = bytes_recd + len(chunk)
                         print('RCVDATA : ' + str(RCVDATA))
                         print(b''.join(RCVDATA))
                         # UDPの受信バッファサイズを更新
                         us.UDP_RCV_BUFF = int.from_bytes(b''.join(RCVDATA), sys.byteorder)
                         print('TCP_SERVER_RUN/UDP_RCV_BUFF : ' + str(us.UDP_RCV_BUFF))
                     
                     # 1（生存監視)
                     elif RCV_TYPE == b'\x01':
                         self.kill_flag = False
                     # 2（終了通知）
                     elif RCV_TYPE == b'\x02':
                         self.kill_flag = True
                     else:
                         self.kill_flag = True
             except socket.timeout:
                 print('timeout')
                 self.TCP_SOCKET

# UDPサーバクラス
# 画像の受信、表示を行う
class UDP_SERVER(threading.Thread):
    # 初期化処理
    def __init__(self, PORT):
        threading.Thread.__init__(self)
        self.kill_flag = False

        self.HOST = socket.gethostname()
        self.PORT = PORT

        self.UDP_SOCKET = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.UDP_RCV_BUFF = self.UDP_SOCKET.getsockopt(socket.SOL_SOCKET, socket.SO_RCVBUF)
        print('UDP_SERVER_INIT/UDP_RCV_BUFF : ' + str(self.UDP_RCV_BUFF))

    # 受信待ち
    def run(self):
        # UDPサーバ開始
        self.UDP_SOCKET.bind((gethostbyname(self.HOST), self.PORT))
        #while self.kill_flag == False :


if __name__ == '__main__':
    ts = TCP_SERVER(TCP_RCV_PORT)
    ts.setDaemon(True)
    ts.start()

    us = UDP_SERVER(UDP_RCV_PORT)
    us.setDaemon(True)

    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as sock:
        sock.settimeout(5)
        # サーバを指定
        sock.connect((TCP_SND_IP, TCP_SND_PORT))

        # サーバにメッセージを送る
        # メッセージ区分０(1byte)
        # ディスプレイID(1byte)
        # TCP受信ポート番号（2byte）
        # UDP受信ポート番号（2byte）
        # UDP受信バッファサイズ（4byte）
        print('MAIN/UDP_RCV_BUFF : ' + str(us.UDP_RCV_BUFF))
        s = struct.Struct('<BBHHI')
        SNDDATA = s.pack( 0, DISP_ID, TCP_RCV_PORT, UDP_RCV_PORT, us.UDP_RCV_BUFF)
        print('SNDDATA(packed) : ' + str(SNDDATA))
        print('SNDDATA(unpacked) : ' + str(s.unpack(SNDDATA)))
        sock.send(SNDDATA)
    ts.join()

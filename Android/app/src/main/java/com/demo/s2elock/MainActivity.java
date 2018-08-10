package com.demo.s2elock;

import android.os.AsyncTask;
import android.os.Bundle;
import android.support.v7.app.AppCompatActivity;
import android.util.Log;

import java.io.BufferedWriter;
import java.io.DataOutputStream;
import java.io.IOException;
import java.io.OutputStreamWriter;
import java.io.PrintWriter;
import java.net.InetAddress;
import java.net.Socket;
import java.net.UnknownHostException;

import cheekiat.slideview.SlideView;

public class MainActivity extends AppCompatActivity {

    SlideView mSlideView;
    private Socket socket;
    private static final int SERVERPORT = 80;
    private static final String SERVER_IP = "192.168.1.90";


    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);

        mSlideView = (SlideView) findViewById(R.id.slide_view);
        mSlideView.setOnFinishListener(new SlideView.OnFinishListener() {
            @Override
            public void onFinish() {
            new ConnectTask().execute();
            mSlideView.reset();

            }
        });



    }

    public class ConnectTask extends AsyncTask<String, String, String> {

        @Override
        protected String doInBackground(String... message) {
            try {
                InetAddress serverAddr = InetAddress.getByName(SERVER_IP);

                socket = new Socket(serverAddr, SERVERPORT);
                if(socket.isConnected()) {
                    PrintWriter out = new PrintWriter(new BufferedWriter(
                            new OutputStreamWriter(socket.getOutputStream())),
                            true);
                    out.println("ON");
                    out.flush();
                    out.close();
                    socket.close();
                    socket = null;
                }
                else
                {
                    Log.d("doh", "not conneected");
                }
            } catch (UnknownHostException e) {
                e.printStackTrace();
            } catch (IOException e) {
                e.printStackTrace();
            } catch (Exception e) {
                e.printStackTrace();
            }

            return null;
        }

        @Override
        protected void onProgressUpdate(String... values) {
            super.onProgressUpdate(values);
            //response received from server
            Log.d("test", "done ");
            //process server response here....

        }
    }

}

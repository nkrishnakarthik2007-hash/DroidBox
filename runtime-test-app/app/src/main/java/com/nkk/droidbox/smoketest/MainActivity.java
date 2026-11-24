package com.nkk.droidbox.smoketest;

import android.app.Activity;
import android.os.Bundle;
import android.view.Gravity;
import android.view.View;
import android.widget.Button;
import android.widget.LinearLayout;
import android.widget.TextView;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.nio.charset.StandardCharsets;

public final class MainActivity extends Activity {
    private TextView status;
    private int clicks;

    @Override public void onCreate(Bundle state) {
        super.onCreate(state);
        clicks = getPreferences(MODE_PRIVATE).getInt("clicks", 0);
        LinearLayout root = new LinearLayout(this);
        root.setOrientation(LinearLayout.VERTICAL);
        root.setGravity(Gravity.CENTER);
        root.setPadding(28, 28, 28, 28);
        TextView title = new TextView(this);
        title.setText("DroidBox Android Runtime Test");
        title.setTextSize(23);
        title.setGravity(Gravity.CENTER);
        status = new TextView(this);
        status.setTextSize(16);
        status.setGravity(Gravity.CENTER);
        status.setPadding(0, 24, 0, 24);
        Button counter = new Button(this);
        counter.setText("Tap to test input + storage");
        counter.setOnClickListener(v -> {
            clicks++;
            getPreferences(MODE_PRIVATE).edit().putInt("clicks", clicks).apply();
            status.setText("Button taps saved: " + clicks + "\nSharedPreferences: PASS");
        });
        Button file = new Button(this);
        file.setText("Test private file access");
        file.setOnClickListener(v -> testPrivateFile());
        root.addView(title);
        root.addView(status);
        root.addView(counter);
        root.addView(file);
        setContentView(root);
        status.setText("Ready. Previous taps: " + clicks + "\nLaunch + Activity lifecycle: PASS");
    }

    private void testPrivateFile() {
        try {
            File target = new File(getFilesDir(), "droidbox-smoke.txt");
            byte[] expected = "DroidBox private storage works".getBytes(StandardCharsets.UTF_8);
            try (FileOutputStream out = new FileOutputStream(target)) { out.write(expected); }
            byte[] actual = new byte[expected.length];
            try (FileInputStream in = new FileInputStream(target)) {
                int count = in.read(actual);
                if (count != expected.length) throw new IllegalStateException("Short file read");
            }
            status.setText(new String(actual, StandardCharsets.UTF_8).equals(new String(expected, StandardCharsets.UTF_8))
                    ? "Private app file read/write: PASS" : "Private app file read/write: FAIL");
        } catch (Exception e) {
            status.setText("Private file test failed: " + e.getClass().getSimpleName());
        }
    }
}

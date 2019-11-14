using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockBehavior : MonoBehaviour
{
    private Material mat;
    private int block_id; // Bottom (0) to top 
    private double last_hit_ts = 0;
    private bool flashing = false;
    private bool even = false;
    Color FLASH_COLOR = Color.red;
    Color DEF_COLOR = Color.gray;
    // Start is called before the first frame update
    void Start()
    {
        mat = GetComponent<Renderer>().material;
        mat.color = this.defaultColor();
    }

    // Update is called once per frame
    void Update()
    {
        if (flashing) {
            double secs_since_flash = Util.timestamp() - last_hit_ts;
            if (secs_since_flash > 1) Unflash();
        }
        
    }

    public int getBlockId() {
        return this.block_id;
    }

    Color defaultColor() {
        return even ? Color.gray : new Color(.5f, .5f, .7f);
    }

    public void setID(int id) {
        this.block_id = id;
        this.even = id % 2 == 0;
    }

    public void Flash() {
        last_hit_ts = Util.timestamp();
        mat.color = FLASH_COLOR;
        flashing = true;
    }

    void Unflash() {
        last_hit_ts = 0;
        mat.color = defaultColor();
        flashing = false;
    }
}

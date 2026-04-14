-- ============================================================
-- ToolPool Supabase Migration
-- Run this in Supabase Dashboard > SQL Editor
-- ============================================================

-- 1. Add new columns to existing Tools table
ALTER TABLE "Tools"
  ADD COLUMN IF NOT EXISTS category TEXT DEFAULT '',
  ADD COLUMN IF NOT EXISTS owner_id TEXT DEFAULT '',
  ADD COLUMN IF NOT EXISTS owner_name TEXT DEFAULT '',
  ADD COLUMN IF NOT EXISTS neighborhood TEXT DEFAULT '',
  ADD COLUMN IF NOT EXISTS image_url TEXT DEFAULT '';

-- 2. Create Interest_Submissions table
CREATE TABLE IF NOT EXISTS "Interest_Submissions" (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tool_id UUID NOT NULL REFERENCES "Tools"(id) ON DELETE CASCADE,
  tool_name TEXT NOT NULL DEFAULT '',
  renter_id TEXT NOT NULL DEFAULT '',
  owner_id TEXT NOT NULL DEFAULT '',
  message TEXT DEFAULT '',
  start_date TEXT,
  end_date TEXT,
  channel_url TEXT,
  status TEXT NOT NULL DEFAULT 'pending',
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- 3. Enable RLS (Row Level Security) on new table
ALTER TABLE "Interest_Submissions" ENABLE ROW LEVEL SECURITY;

-- Allow anon reads (adjust for your auth needs)
CREATE POLICY "Allow anon read interest_submissions"
  ON "Interest_Submissions"
  FOR SELECT
  USING (true);

-- Allow service role inserts
CREATE POLICY "Allow service role insert interest_submissions"
  ON "Interest_Submissions"
  FOR INSERT
  WITH CHECK (true);

-- 4. Optional: Seed the Tools table with sample data
-- (You can also use the /api/Tools/seed endpoint instead)
--
-- INSERT INTO "Tools" (name, description, price, category, owner_id, owner_name, neighborhood) VALUES
--   ('DeWalt Power Drill', 'Cordless 20V drill with two batteries and charger', 12.00, 'Power Tools', 'owner-1', 'Mike T.', 'Southend'),
--   ('Circular Saw', '7-1/4 inch blade, great for framing and decking', 18.00, 'Power Tools', 'owner-2', 'Emma L.', 'Westside'),
--   ('Hand Tool Set', 'Complete 50-piece set with wrenches, pliers, and screwdrivers', 8.00, 'Hand Tools', 'owner-3', 'James K.', 'Midtown'),
--   ('Ladder (20ft)', 'Extension ladder, aluminum, supports up to 250 lbs', 15.00, 'Equipment', 'owner-1', 'Mike T.', 'Downtown'),
--   ('Pressure Washer', '2000 PSI electric pressure washer with hose and nozzles', 25.00, 'Cleaning', 'owner-4', 'Sarah M.', 'Eastside'),
--   ('Hedge Trimmer', '24-inch cordless hedge trimmer, battery included', 10.00, 'Garden', 'owner-2', 'Emma L.', 'Uptown'),
--   ('Tile Cutter', 'Manual tile cutter for ceramic and porcelain up to 24 inches', 14.00, 'Hand Tools', 'owner-5', 'Chen W.', 'Downtown'),
--   ('Shop Vacuum', '6-gallon wet/dry shop vac with attachments', 9.00, 'Cleaning', 'owner-3', 'James K.', 'Southend');

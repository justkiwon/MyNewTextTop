import { createClient } from '@supabase/supabase-js';

const supabaseUrl = import.meta.env.VITE_SUPABASE_URL ?? '';
const supabaseAnonKey = import.meta.env.VITE_SUPABASE_ANON_KEY ?? '';

// Only the public anon/publishable key belongs in the browser. RLS policies in
// Supabase decide which rows this client may read or change.
export const supabase = createClient(supabaseUrl, supabaseAnonKey);

export const isSupabaseConfigured =
  Boolean(supabaseUrl) &&
  Boolean(supabaseAnonKey) &&
  !supabaseUrl.includes('YOUR_PROJECT_REF') &&
  !supabaseAnonKey.includes('YOUR_SUPABASE');

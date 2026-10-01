"use client";
import { Box, Typography } from "@mui/material";

export default function NavGroup({ label, hideText }: { label: string; hideText: boolean }) {
  if (hideText) {
    return (
      <Box sx={{ height: "1px", bgcolor: "divider", my: 1.5, mx: "auto", width: "20px" }} />
    );
  }

  return (
    <Box sx={{ px: 2, mt: 2.5, mb: 0.5 }}>
      <Typography
        variant="caption"
        sx={{ fontWeight: 700, letterSpacing: "0.5px", textTransform: "uppercase", color: "text.secondary" }}
      >
        {label}
      </Typography>
    </Box>
  );
}
